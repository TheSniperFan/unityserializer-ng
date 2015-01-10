using System;
using System.Reflection;

namespace Serialization
{
    public class Entry
    {
        /// <summary>
        /// The name of the item being read or written
        /// This should be filled out by the storage when 
        /// MustHaveName = true and deserializing
        /// </summary>
        public string Name;
        /// <summary>
        /// The type of the item being stored or retrieved
        /// this should be filled out by the storage when
        /// MustHaveName=true and deserializing. Will
        /// be filled in when serializing.
        /// </summary>
        private PropertyInfo _propertyInfo;
        private FieldInfo _fieldInfo;
        public Type StoredType;
        /// <summary>
        /// On writing, the value of the object for reference, not needed on
        /// deserialization 
        /// </summary>
        public object Value;
		
        /// <summary>
        /// Indicates whether this entry is static
        /// </summary>
        public bool IsStatic;
        /// <summary>
        /// Set to indicate that the name provided is that of a field or property
        /// and is needed to reset the value later
        /// </summary>
        public bool MustHaveName;
        /// <summary>
        /// The type of the object which owns the item being serialized or null
        /// if not directly owned.  This will always be set on serialization and 
        /// deserialization when MustHaveName = true and can be used to 
        /// look up field and property information. Or you can ignore it if 
        /// you don't need it
        /// </summary>
        public Type OwningType;
        /// <summary>
        /// The property info or null, if the value did not
        /// come from a property.  You might want to use
        /// the to look up attributes attached to the property
        /// definition
        /// </summary>
        public PropertyInfo PropertyInfo
        {
            get { return _propertyInfo; }
            set
            {
                Name = value.Name;
                StoredType = value.PropertyType;

                _propertyInfo = value;
            }
        }
        /// <summary>
        /// The field info or null, if the value did not
        /// come from a field. You might want to use it
        /// to look up attributes attached to the field definition
        /// </summary>
        public FieldInfo FieldInfo
        {
            get
            {

                return _fieldInfo;
            }
            set
            {
                Name = value.Name;
                StoredType = value.FieldType;
                _fieldInfo = value;
            }
        }

        public GetSet Setter;
    }
}