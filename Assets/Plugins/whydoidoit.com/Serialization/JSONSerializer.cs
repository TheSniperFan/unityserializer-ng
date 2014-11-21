using System;
using System.Collections.Generic;
using System.Text;
using LitJson;

namespace Serialization
{
    public class JSONSerializer : IStorage
    {
        public static string source = string.Empty;
        private readonly JsonReader _reader;
        private StringBuilder _json;

        public JSONSerializer()
        {
        }

        public JSONSerializer(string json)
        {
            _reader = new JsonReader(json);
        }

        public string Data
        {
            get
            {
                var data = _json.ToString().Replace(",]", "]").Replace(",}", "}");
                return data;
            }
        }

        #region IStorage implementation

        private static bool _isReference;
        private static int _reference;
        private static string _currentType;

        public void StartSerializing()
        {
            _json = new StringBuilder();
        }

        public void FinishedSerializing()
        {
        }

        public void FinishedDeserializing()
        {
        }


        public bool StartSerializing(Entry entry, int id)
        {
            return false;
        }

        public void FinishSerializing(Entry entry)
        {
        }

        public object StartDeserializing(Entry entry)
        {
            _reader.Read();
            if (_reader.Token == JsonToken.ObjectStart)
            {
                _reader.Read();
                if ((string) _reader.Value == "___o")
                {
                    _reader.Read();
                    _isReference = true;
                    _reference = (int) _reader.Value;
                }
                else
                {
					_isReference = false;
                    _reference = -1;
                    _reader.Read();
                    _currentType = (string) _reader.Value;
                    entry.StoredType = UnitySerializer.GetTypeEx(_currentType);
                }
                return null;
            }
            return _reader.Value;
        }

        public void DeserializeGetName(Entry entry)
        {
        }

        public void FinishDeserializing(Entry entry)
        {
        }

        public Entry[] ShouldWriteFields(Entry[] fields)
        {
            return fields;
        }

        public Entry[] ShouldWriteProperties(Entry[] properties)
        {
            return properties;
        }

        public void StartDeserializing()
        {
        }

        public Entry BeginReadProperty(Entry entry)
        {
            _reader.Read();
            entry.Name = (string) _reader.Value;
            return entry;
        }

        public void EndReadProperty()
        {
        }

        public Entry BeginReadField(Entry entry)
        {
            _reader.Read();
            entry.Name = (string) _reader.Value;
            return entry;
        }

        public void EndReadField()
        {
        }

        public int BeginReadProperties()
        {
            return 0;
        }
		
		public bool HasMore()
		{
			_reader.Read();
			if(_reader.Token == JsonToken.ArrayEnd || _reader.Token == JsonToken.ObjectEnd)
				return false;
			_reader.reReadToken = true;
			return true;
		}

        public int BeginReadFields()
        {
			return -1;
        }

        public void EndReadProperties()
        {
			
        }

        public void EndReadFields()
        {
			_reader.reReadToken = true;
        }

        public T ReadSimpleValue<T>()
        {
            return (T) ReadSimpleValue(typeof (T));
        }

        public object ReadSimpleValue(Type type)
        {
            switch (type.Name)
            {
                case "DateTime":
                    return DateTime.Parse((string) _reader.Value);
                case "String":
                    return UnitySerializer.UnEscape((string) _reader.Value);
                default:
                    return _reader.Value;
            }
        }

        public bool IsMultiDimensionalArray(out int length)
        {
            length = -1;
            _reader.Read();
			_reader.reReadToken = true;
			return (string)_reader.Value == "dimensions";
        }

        public void BeginReadMultiDimensionalArray(out int dimension, out int count)
        {
            _reader.Read();
			_reader.Read();
			dimension = (int)_reader.Value;
			_reader.Read();
			_reader.Read();
			count = (int)_reader.Value;
        }

        public void EndReadMultiDimensionalArray()
        {
            
        }

        public int ReadArrayDimension(int index)
        {
            _reader.Read();
			_reader.Read();
			return (int)_reader.Value;
        }

        public Array ReadSimpleArray(Type elementType, int count)
        {
            _reader.Read();
            _reader.Read();
            count = (int) _reader.Value;
            var array = Array.CreateInstance(elementType, new[] {(long) count});
            _reader.Read();
            _reader.Read();
            if (_reader.Token == JsonToken.ArrayStart)
            {
                for (var i = 0; i < count; i++)
                {
                    _reader.Read();
                    array.SetValue(Convert.ChangeType(_reader.Value, elementType), i);
                }
                _reader.Read();
            }
            return array;
        }


        public int BeginReadObject(out bool isReference)
        {
            if (_isReference)
            {
                isReference = true;
                return _reference;
            }
            isReference = false;
            return 0;
        }

        public void EndReadObject()
        {
            _reader.Read(); //Read the close of the object
        }

        public int BeginReadList(Type valueType)
        {
            _reader.Read();
            if ((string) _reader.Value == "___contents")
            {
                _reader.Read();
                return -1;
            }
            return 0;
        }

        public object BeginReadListItem(int index, Entry entry)
        {
			return null;
        }

        public void EndReadListItem()
        {
        }

        public void EndReadList()
        {
        }
		
		
        public int BeginReadDictionary(Type keyType, Type valueType)
        {
            return -1;
        }

        public object BeginReadDictionaryKeyItem(int index, Entry entry)
        {
			return null;
        }

        public void EndReadDictionaryKeyItem()
        {
        }

        public object BeginReadDictionaryValueItem(int index, Entry entry)
        {
            return null;
        }

        public void EndReadDictionaryValueItem()
        {
        }

        public void EndReadDictionary()
        {
        }

        public int BeginReadObjectArray(Type valueType)
        {
			
            _reader.Read();
            _reader.Read(); //Array start
			var count = (int)_reader.Value;
			_reader.Read();
			_reader.Read();
            return count;
        
        
        }

        public object BeginReadObjectArrayItem(int index, Entry entry)
        {
			return null;
        }

        public void EndReadObjectArrayItem()
        {
        }

        public void EndReadObjectArray()
        {
			_reader.Read();
        }

        public void BeginWriteObject(int id, Type objectType, bool wasSeen)
        {
            if (!wasSeen)
            {
                _json.AppendFormat("{{\"___i\":\"{0}\",", objectType.FullName);
            }
            else
            {
                _json.AppendFormat("{{\"___o\":{0},", id);
            }
        }

        public void EndWriteObject()
        {
            _json.Append("}");
        }

        public void BeginWriteList(int count, Type listType)
        {
            _json.Append("\"___contents\":[");
        }

        public bool BeginWriteListItem(int index, object value)
        {
			return false;
        }

        public void EndWriteListItem()
        {
            _json.Append(",");
        }

        public void EndWriteList()
        {
            _json.Append("],");
        }

        public void BeginWriteObjectArray(int count, Type arrayType)
        {
			if(_multiDimensional > 0)
			{
				_json.AppendFormat("\"count\":{1},\"contents{0}\":[", _multiDimensional, count);
			}
			else
			{
            	_json.Append("\"count\":" + count + ",\"contents\":[");
			}
        }

        public bool BeginWriteObjectArrayItem(int index, object value)
        {
			return false;
        }

        public void EndWriteObjectArrayItem()
        {
            _json.Append(",");
        }

        public void EndWriteObjectArray()
        {
            _json.Append("],");
        }
		
		static int _multiDimensional;
				
		
        public void BeginMultiDimensionArray(Type arrayType, int dimensions, int count)
        {
			_multiDimensional++;
            _json.AppendFormat("\"dimensions\":{0},\"count\":{1},", dimensions, count);
        }

        public void EndMultiDimensionArray()
        {
            _multiDimensional--;
        }

        public void WriteArrayDimension(int index, int count)
        {
            _json.AppendFormat("\"dimension{0}\":{1},", index, count);
        }
		
		private static int _arrayCount;
		
        public void WriteSimpleArray(int count, Array array)
        {
			if(_multiDimensional>0)
			{
	            _json.AppendFormat("\"count{1}\":{0},", count, _arrayCount);
	            _json.AppendFormat("\"contents{0}\":[", _arrayCount++);
			}
			else
			{
	            _json.AppendFormat("\"count\":{0},", count);
	            _json.Append("\"contents\":[");
			}
            var first = true;
            foreach (var value in array)
            {
                if (!first)
                {
                    _json.Append(",");
                }
                first = false;
                WriteSimpleValue(value);
            }

            _json.Append("],");
        }

        public void WriteSimpleValue(object value)
        {
            if (value is string)
            {
                /*if(((string)value).Length > 201) {
					var val = (string)value;
					var data = UnitySerializer.Escape((string)value);
					data = data.Substring(data.Length - 200);
					val = val.Substring(val.Length - 200);
					UnityEngine.Debug.Log(data + "\n\n\n" + val);
				} */
                _json.AppendFormat("\"{0}\"", UnitySerializer.Escape((string) value));

                /*var upd = _json.ToString();
				if(_json.Length>20)
				{
					upd = upd.Substring(upd.Length-20);
					UnityEngine.Debug.Log(upd);
				}*/
            }
            else if (value is DateTime)
            {
                _json.AppendFormat("\"{0}\"", value);
            }
            else if (value is bool)
            {
                _json.Append(((bool) value) ? "true" : "false");
            }
            else if (value is float || value is double)
            {
                _json.AppendFormat("{0:0.00000000}", value);
            }
            else
            {
                _json.AppendFormat("{0}", value);
            }
        }

        public void BeginWriteDictionary(int count, Type dictionaryType)
        {
         
        }

        public bool BeginWriteDictionaryKey(int id, object value)
        {
           // _json.AppendFormat("\"k{0}\":", id);
			return false;
        }

        public void EndWriteDictionaryKey()
        {
            _json.AppendFormat(",");
        }

        public bool BeginWriteDictionaryValue(int id, object value)
        {
            //_json.AppendFormat("\"v{0}\":", id);
			return false;
        }

        public void EndWriteDictionaryValue()
        {
            _json.AppendFormat(",");
        }

        public void EndWriteDictionary()
        {
        }

        public void BeginWriteProperties(int count)
        {
        }

        public void EndWriteProperties()
        {
        }

        public void BeginWriteProperty(string name, Type type)
        {
            _json.AppendFormat("\"{0}\":", name);
        }

        public void EndWriteProperty()
        {
            _json.AppendFormat(",");
        }

        public void BeginWriteFields(int count)
        {
        }

        public void EndWriteFields()
        {
        }

        public void BeginWriteField(string name, Type type)
        {
            _json.AppendFormat("\"{0}\":", name);
        }

        public void EndWriteField()
        {
            _json.AppendFormat(",");
        }

        public void BeginOnDemand(int id)
        {
        }

        public void EndOnDemand()
        {
        }

        public bool SupportsOnDemand
        {
            get
            {
                return false;
            }
        }

       	public void BeginReadDictionaryKeys ()
		{
			_reader.Read();
			if((string)_reader.Value == "___keys")
			{
				_reader.Read();
			}
		}

		public void EndReadDictionaryKeys ()
		{
		}

		public void BeginReadDictionaryValues ()
		{
			_reader.Read();
			if((string)_reader.Value=="___values")
			{
				_reader.Read();
			}
		}

		public void EndReadDictionaryValues ()
		{
		}
		
		public void BeginWriteDictionaryKeys ()
		{
			_json.Append("\"___keys\": [");
			
		}

		public void EndWriteDictionaryKeys ()
		{
			_json.Append("],");
		}

		public void BeginWriteDictionaryValues ()
		{
			_json.Append("\"___values\": [");
		}

		public void EndWriteDictionaryValues ()
		{
			_json.Append("],");
			
		}
		#endregion
    }
}