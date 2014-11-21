using System;
using System.IO;
using System.Linq;

namespace Serialization
{
    public class BinarySerializer : IStorage
    {
        public byte[] Data { get; private set; }
        private MemoryStream _myStream;


        /// <summary>
        /// Used when serializing
        /// </summary>
        public BinarySerializer()
        {

        }
        /// <summary>
        /// Used when deserializaing
        /// </summary>
        /// <param name="data"></param>
        public BinarySerializer(byte[] data)
        {
            Data = data;
        }


        #region writing

        private BinaryWriter _writer;
        private void EncodeType(object item, Type storedType)
        {

            if (item == null)
            {
                WriteSimpleValue((ushort)0xFFFE);
                return;
            }

            var itemType = item.GetType();

            //If this isn't a simple type, then this might be a subclass so we need to
            //store the type 
            if (storedType == null || storedType != item.GetType() || UnitySerializer.Verbose)
            {
                //Write the type identifier
                var tpId = UnitySerializer.GetTypeId(itemType);
                WriteSimpleValue(tpId);
            }
            else
                //Write a dummy identifier
                WriteSimpleValue((ushort)0xFFFF);

        }

        public bool StartSerializing(Entry entry, int id)
        {
            if (entry.MustHaveName)
            {
                ushort nameID = UnitySerializer.GetPropertyDefinitionId(entry.Name);
                WriteSimpleValue(nameID);
            }
            var item = entry.Value ?? new UnitySerializer.Nuller();
            EncodeType(item, entry.StoredType);
            return false;
        }







        public void StartSerializing()
        {
            _myStream = new MemoryStream();
            _writer = new BinaryWriter(_myStream);
			UnitySerializer.PushKnownTypes();
            UnitySerializer.PushPropertyNames();
        }





        public void FinishedSerializing()
        {
            _writer.Flush();
            _writer.Close();
            _myStream.Flush();
            var data = _myStream.ToArray();
            _myStream.Close();
            _myStream = null;

            var stream = new MemoryStream();
            var outputWr = new BinaryWriter(stream);
            outputWr.Write("SerV10");
            //New, store the verbose property
            outputWr.Write(UnitySerializer.Verbose);
			if(UnitySerializer.SerializationScope.IsPrimaryScope)
			{
	            outputWr.Write(UnitySerializer._knownTypesLookup.Count);
	            foreach (var kt in UnitySerializer._knownTypesLookup.Keys)
	            {
					outputWr.Write(kt.FullName);
	            }
	            outputWr.Write(UnitySerializer._propertyLookup.Count);
	            foreach (var pi in UnitySerializer._propertyLookup.Keys)
	            {
	                outputWr.Write(pi);
	            }
			}
			else
			{
				outputWr.Write(0);
				outputWr.Write(0);
			}
			outputWr.Write(data.Length);
            outputWr.Write(data);
            outputWr.Flush();
            outputWr.Close();
            stream.Flush();

            Data = stream.ToArray();
            stream.Close();
            _writer = null;
            _reader = null;
			
			UnitySerializer.PopKnownTypes();
            UnitySerializer.PopPropertyNames();

        }



        public bool SupportsOnDemand
        {
            get { return false; }
        }
        public void BeginOnDemand(int id) { }
        public void EndOnDemand() { }



        public void BeginWriteObject(int id, Type objectType, bool wasSeen)
        {
			if(objectType == null)
			{
				WriteSimpleValue('X');
			}
            else if (wasSeen)
			{
                WriteSimpleValue('S');
                WriteSimpleValue(id);
            }
            else
            {
                WriteSimpleValue('O');
            }
        }






        public void BeginWriteProperties(int count)
        {
			if(count > 250)
			{
				WriteSimpleValue((byte)255);
				WriteSimpleValue(count);
			}
			else
			{
				WriteSimpleValue((byte)count);
			}
        }
        public void BeginWriteFields(int count)
        {
			if(count > 250)
			{
				WriteSimpleValue((byte)255);
				WriteSimpleValue(count);
			}
			else
			{
				WriteSimpleValue((byte)count);
			}
        }
        public void WriteSimpleValue(object value)
        {
            UnitySerializer.WriteValue(_writer, value);
        }
        public void BeginWriteList(int count, Type listType)
        {
            WriteSimpleValue(count);
        }
        public void BeginWriteDictionary(int count, Type dictionaryType)
        {
            WriteSimpleValue(count);
        }



        public void WriteSimpleArray(int count, Array array)
        {
            WriteSimpleValue(count);

            var elementType = array.GetType().GetElementType();
            if (elementType == typeof(byte))
            {
				UnitySerializer.WriteValue(_writer, array);
            }
            else if (elementType.IsPrimitive)
            {
                var ba = new byte[Buffer.ByteLength(array)];
                Buffer.BlockCopy(array, 0, ba, 0, ba.Length);
                UnitySerializer.WriteValue(_writer, ba);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var v = array.GetValue(i);
					if(v==null)
					{
						UnitySerializer.WriteValue(_writer, (byte)0);
					}
					else
					{
			            UnitySerializer.WriteValue(_writer, (byte)1);
						UnitySerializer.WriteValue(_writer, v);
					}						
					
                }
            }
        }

		
		
        public void BeginMultiDimensionArray(Type arrayType, int dimensions, int count)
        {
			
            WriteSimpleValue(-1);
            WriteSimpleValue(dimensions);
            WriteSimpleValue(count);
        }
        public void WriteArrayDimension(int dimension, int count)
        {
            WriteSimpleValue(count);
        }
        public void BeginWriteObjectArray(int count, Type arrayType)
        {
            WriteSimpleValue(count);
        }

        public Entry[] ShouldWriteFields(Entry[] fields) { return fields; }
        public Entry[] ShouldWriteProperties(Entry[] properties) { return properties; }




        #endregion writing



        #region reading

        private BinaryReader _reader;
        private Type DecodeType(Type storedType)
        {
			try
			{
	            var tid = ReadSimpleValue<ushort>();
				if(tid == 0xffff)
					return storedType;
	            if (tid == 0xFFFE)
	            {
	                return null;
	            }
				if(tid >= 60000)
				{
					try
					{
						return UnitySerializer.PrewarmLookup[tid-60000];
					}
					catch
					{
						throw new Exception("Data stream appears corrupt, found a TYPE ID of " + tid.ToString());
					}
				}

                storedType = UnitySerializer._knownTypesList[tid];
	            return storedType;
			}
			catch
			{
				return null;
			}

        }

        public void FinishedDeserializing()
        {
            _reader.Close();
            _myStream.Close();
            _reader = null;
            _myStream = null;
            _writer = null;
			
			UnitySerializer.PopKnownTypes();
			UnitySerializer.PopPropertyNames();
        }

        //Gets the name from the stream
        public void DeserializeGetName(Entry entry)
        {
            if (!entry.MustHaveName)
            {
                return;
            }
            var id = ReadSimpleValue<ushort>();
			try
			{
				entry.Name = id >= 50000 ? PreWarm.PrewarmNames[id-50000] : UnitySerializer._propertyList[id];
			}
			catch
			{
				throw new Exception("Data stream may be corrupt, found an id of " + id + " when looking a property name id");
			}
        }

        /// <summary>
        /// Starts to deserialize the object
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public object StartDeserializing(Entry entry)
        {
            var itemType = DecodeType(entry.StoredType);
			entry.StoredType = itemType;
			return null;
        }


        public Entry BeginReadProperty(Entry entry)
        {
            return entry;
        }
        public void EndReadProperty()
        {

        }
        public Entry BeginReadField(Entry entry)
        {
            return entry;
        }
        public void EndReadField()
        {

        }

        public void StartDeserializing()
        {
			UnitySerializer.PushKnownTypes();
            UnitySerializer.PushPropertyNames();

            var stream = new MemoryStream(Data);
            var reader = new BinaryReader(stream);
            var version = reader.ReadString();
            UnitySerializer.currentVersion = int.Parse(version.Substring(4));
            if (UnitySerializer.currentVersion >= 3)
			{
				UnitySerializer.Verbose = reader.ReadBoolean();
			}
	        
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var typeName = reader.ReadString();
                var tp = UnitySerializer.GetTypeEx(typeName);
                if (tp == null)
                {
                    var map = new UnitySerializer.TypeMappingEventArgs
                    {
                        TypeName = typeName
                    };
                    UnitySerializer.InvokeMapMissingType(map);
                    tp = map.UseType;
                }
                if (tp == null)
                    throw new ArgumentException(string.Format("Cannot reference type {0} in this context", typeName));
                UnitySerializer._knownTypesList.Add(tp);
            }
            count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                UnitySerializer._propertyList.Add(reader.ReadString());
            }
		
            var data = reader.ReadBytes(reader.ReadInt32());

            _myStream = new MemoryStream(data);
            _reader = new BinaryReader(_myStream);
            reader.Close();
            stream.Close();
        }

        public void FinishDeserializing(Entry entry) { }





        public Array ReadSimpleArray(Type elementType, int count)
        {
            if (count == -1)
            {
                count = ReadSimpleValue<int>();
            }

            if (elementType == typeof(byte))
            {
                return ReadSimpleValue<byte[]>();
            }
            if (elementType.IsPrimitive && UnitySerializer.currentVersion >= 6)
            {
                var ba = ReadSimpleValue<byte[]>();
                var a = Array.CreateInstance(elementType, count);
                Buffer.BlockCopy(ba, 0, a, 0, ba.Length);
                return a;
            }
            var result = Array.CreateInstance(elementType, count);
			if(UnitySerializer.currentVersion >= 8)
			{
	            for (var l = 0; l < count; l++)
	            {
	                var go = (byte)ReadSimpleValue(typeof(byte));
	                result.SetValue(go != 0 ? ReadSimpleValue(elementType) : null, l);
	            }
			}
			else
			{
				for (var l = 0; l < count; l++)
	            {
	                result.SetValue(ReadSimpleValue(elementType), l);
	            }
            
			}
            return result;
        }

        public int BeginReadProperties()
        {
            var count = ReadSimpleValue<byte>();
            return count==255 ? ReadSimpleValue<int>() : count;
        }

        public int BeginReadFields()
        {
            var count = ReadSimpleValue<byte>();
            return count==255 ? ReadSimpleValue<int>() : count;
        }


        public T ReadSimpleValue<T>()
        {
            return (T)ReadSimpleValue(typeof(T));
        }
        public object ReadSimpleValue(Type type)
        {
            UnitySerializer.ReadAValue read;
            if (!UnitySerializer.Readers.TryGetValue(type, out read))
            {
                return _reader.ReadInt32();
            }
            return read(_reader);

        }

        public bool IsMultiDimensionalArray(out int length)
        {
            var count = ReadSimpleValue<int>();
            if (count == -1)
            {
                length = -1;
                return true;
            }
            length = count;
            return false;
        }

        public int BeginReadDictionary(Type keyType, Type valueType)
        {
            return ReadSimpleValue<int>();
        }
        public void EndReadDictionary() { }

        public int BeginReadObjectArray(Type valueType)
        {
            return ReadSimpleValue<int>();
        }
        public void EndReadObjectArray() { }


        public void BeginReadMultiDimensionalArray(out int dimension, out int count)
        {
            //
            //var dimensions = storage.ReadValue<int>("dimensions");
            //var totalLength = storage.ReadValue<int>("length");
            dimension = ReadSimpleValue<int>();
            count = ReadSimpleValue<int>();
        }
        public void EndReadMultiDimensionalArray() { }

        public int ReadArrayDimension(int index)
        {
            // //.ReadValue<int>("dim_len" + item);
            return ReadSimpleValue<int>();
        }


        public int BeginReadList(Type valueType)
        {
            return ReadSimpleValue<int>();
        }
        public void EndReadList() { }

        
        public int BeginReadObject(out bool isReference)
        {
            int result;
            var knownType = ReadSimpleValue<char>();
			if(knownType == 'X')
			{
				isReference = false;
				return -1;
			}
            if (knownType == 'O')
            {
                result = -1; 
				
                isReference = false;
            }
            else
            {
                result = ReadSimpleValue<int>();
                isReference = true;
            }

            return result;
        }


        #endregion reading



        #region do nothing methods

        public void EndWriteObjectArray() { }
        public void EndWriteList() { }
        public void EndWriteDictionary() { }
        public bool BeginWriteDictionaryKey(int id, object value) { return false; }
        public void EndWriteDictionaryKey() { }
        public bool BeginWriteDictionaryValue(int id, object value) { return false; }
        public void EndWriteDictionaryValue() { }
        public void EndMultiDimensionArray() { }
        public void EndReadObject() { }
        public bool BeginWriteListItem(int index, object value) { return false; }
        public void EndWriteListItem() { }
        public bool BeginWriteObjectArrayItem(int index, object value) { return false; }
        public void EndWriteObjectArrayItem() { }
        public void EndReadProperties() { }
        public void EndReadFields() { }
        public object BeginReadListItem(int index, Entry entry) { return null; }
        public void EndReadListItem() { }
        public object BeginReadDictionaryKeyItem(int index, Entry entry) { return null; }
        public void EndReadDictionaryKeyItem() { }
        public object BeginReadDictionaryValueItem(int index, Entry entry) { return null; }
        public void EndReadDictionaryValueItem() { }
        public object BeginReadObjectArrayItem(int index, Entry entry) { return null; }
        public void EndReadObjectArrayItem() { }
        public void EndWriteObject() { }
        public void BeginWriteProperty(string name, Type type) { }
        public void EndWriteProperty() { }
        public void BeginWriteField(string name, Type type) { }
        public void EndWriteField() { }
        public void EndWriteProperties() { }
        public void EndWriteFields() { }
        public void FinishSerializing(Entry entry) { }


        public void BeginReadDictionaryKeys ()
		{
		
		}

		public void EndReadDictionaryKeys ()
		{
		
		}

		public void BeginReadDictionaryValues ()
		{
		
		}

		public void EndReadDictionaryValues ()
		{
		
		}

		public void BeginWriteDictionaryKeys ()
		{
		
		}

		public void EndWriteDictionaryKeys ()
		{
		
		}

		public void BeginWriteDictionaryValues ()
		{
		
		}

		public void EndWriteDictionaryValues ()
		{
		
		}
		public bool HasMore ()
		{
			throw new NotImplementedException ();
		}
		#endregion
    }


}
