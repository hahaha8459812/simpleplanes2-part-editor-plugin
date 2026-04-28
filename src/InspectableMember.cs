using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimplePlanes2PartEditor
{
    internal sealed class InspectableMember
    {
        private readonly object _target;
        private readonly PropertyInfo _property;
        private readonly FieldInfo _field;
        private readonly FieldInfo _backingField;
        private readonly Type _valueType;
        private string _lastCommittedValue;

        public InspectableMember(object target, PropertyInfo property, FieldInfo backingField, string name, string typeName, string access, string value, string attributes)
        {
            _target = target;
            _property = property;
            _backingField = backingField;
            Name = name;
            TypeName = typeName;
            Access = access;
            Value = value;
            EditorValue = value;
            Attributes = attributes;
            _valueType = property.PropertyType;
            CanWrite = ValueConverter.IsEditableType(_valueType) &&
                       (property.GetSetMethod(true) != null || IsWritableBackingField(backingField));
            _lastCommittedValue = value;
        }

        public InspectableMember(object target, FieldInfo field, string name, string typeName, string access, string value, string attributes)
        {
            _target = target;
            _field = field;
            Name = name;
            TypeName = typeName;
            Access = access;
            Value = value;
            EditorValue = value;
            Attributes = attributes;
            _valueType = field.FieldType;
            CanWrite = !field.IsLiteral && ValueConverter.IsEditableType(_valueType);
            _lastCommittedValue = value;
        }

        public string Name { get; private set; }

        public string TypeName { get; private set; }

        public string Access { get; private set; }

        public string Value { get; private set; }

        public string EditorValue { get; set; }

        public string Attributes { get; private set; }

        public bool CanWrite { get; private set; }

        public object TargetObject
        {
            get { return _target; }
        }

        public IEnumerable<string> GetRuntimeRefreshMemberNames()
        {
            List<string> names = new List<string>();
            AddUniqueName(names, Name);

            if (!string.IsNullOrEmpty(Name) && !Name.StartsWith("_", StringComparison.Ordinal))
            {
                AddUniqueName(names, "_" + char.ToLowerInvariant(Name[0]) + Name.Substring(1));
            }

            return names;
        }

        public string Error { get; private set; }

        public bool IsDirty
        {
            get { return !string.Equals(EditorValue, _lastCommittedValue, StringComparison.Ordinal); }
        }

        public bool Matches(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return true;
            }

            return Contains(Name, searchTerm) ||
                   Contains(TypeName, searchTerm) ||
                   Contains(Value, searchTerm) ||
                   Contains(Attributes, searchTerm);
        }

        public bool TryApply()
        {
            object convertedValue;
            string error;

            Error = string.Empty;
            if (!CanWrite)
            {
                Error = "readonly";
                return false;
            }

            if (!ValueConverter.TryConvert(EditorValue, _valueType, GetCurrentValue(), out convertedValue, out error))
            {
                Error = error;
                return false;
            }

            try
            {
                if (_property != null)
                {
                    MethodInfo setter = _property.GetSetMethod(true);
                    if (setter != null)
                    {
                        _property.SetValue(_target, convertedValue, null);
                    }
                    else if (_backingField != null)
                    {
                        _backingField.SetValue(_target, convertedValue);
                    }
                }
                else
                {
                    _field.SetValue(_target, convertedValue);
                }

                RefreshValue();
                return true;
            }
            catch (Exception exception)
            {
                Error = exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        public void ResetEditorValue()
        {
            EditorValue = _lastCommittedValue;
            Error = string.Empty;
        }

        public void RefreshValue()
        {
            object currentValue = GetCurrentValue();
            Value = ValueFormatter.FormatValue(currentValue, _valueType);
            EditorValue = Value;
            _lastCommittedValue = Value;
            Error = string.Empty;
        }

        private object GetCurrentValue()
        {
            if (_property != null)
            {
                return _property.GetValue(_target, null);
            }

            return _field.GetValue(_target);
        }

        private static bool IsWritableBackingField(FieldInfo field)
        {
            return field != null && !field.IsLiteral;
        }

        private static bool Contains(string source, string searchTerm)
        {
            return source != null && source.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddUniqueName(List<string> names, string name)
        {
            if (!string.IsNullOrEmpty(name) && !names.Contains(name))
            {
                names.Add(name);
            }
        }
    }
}
