public class JsonValue : IJsonPrimitive, IConvertible
{
    public TypeCode TypeCode { get; internal set; }
    public Type Type { get; internal set; }
    public string Name { get; set; }
    public IJson Parent { get; set; }
    public object Value { get; set; }
    public bool IsArray => false;
    public bool IsObject => false;
    public bool IsValue => true;
    public bool IsEmpty => (Value is string text ? string.IsNullOrEmpty(text) : Value == null);

    internal static JsonValue Create(string name, object value)
    {
        if (value is JsonValue json)
            return new JsonValue
            {
                TypeCode = json.TypeCode,
                Type = json.Type,
                Name = name,
                Value = json.Value
            };
        return Create(name, value?.GetType() ?? typeof(void), value);
    }

    internal static JsonValue Create(string name, Type type, object value)
    {
        TypeCode typeCode = TypeCode.Empty;
        if (type == null && string.IsNullOrWhiteSpace(name) && value == null)
            return null;

        if (Types.BOOL.Equals(type)) typeCode = TypeCode.Boolean;
        else if (Types.BYTE.Equals(type)) typeCode = TypeCode.Byte;
        else if (Types.CHAR.Equals(type)) typeCode = TypeCode.Char;
        else if (Types.DATETIME.Equals(type)) typeCode = TypeCode.DateTime;
        else if (Types.DECIMAL.Equals(type)) typeCode = TypeCode.Decimal;
        else if (Types.DOUBLE.Equals(type)) typeCode = TypeCode.Double;
        else if (Types.SHORT.Equals(type)) typeCode = TypeCode.Int16;
        else if (Types.INT.Equals(type)) typeCode = TypeCode.Int32;
        else if (Types.LONG.Equals(type)) typeCode = TypeCode.Int64;
        else if (Types.SBYTE.Equals(type)) typeCode = TypeCode.SByte;
        else if (Types.FLOAT.Equals(type)) typeCode = TypeCode.Single;
        else if (Types.STRING.Equals(type)) typeCode = TypeCode.String;
        else if (Types.USHORT.Equals(type)) typeCode = TypeCode.UInt16;
        else if (Types.UINT.Equals(type)) typeCode = TypeCode.UInt32;
        else if (Types.ULONG.Equals(type)) typeCode = TypeCode.UInt64;
        else if (Types.BYTE_ARR.Equals(type)) typeCode = (TypeCode)20;

        return new JsonValue
        {
            TypeCode = typeCode,
            Type = type,
            Name = name,
            Value = value
        };
    }


    public TypeCode GetTypeCode()
    {
        return TypeCode;
    }

    public bool ToBoolean(IFormatProvider provider) => Convert.ToBoolean(Value, provider);
    public static explicit operator bool(JsonValue value)
    {
        if (value == null
         || value.Value == null
         || value.Value.Equals(0)
         || value.Value.Equals(false)
         || value.Value.Equals("false")
         || value.Value.Equals("no")
         || value.Value.Equals("")
         || value.Value.Equals("0"))
            return false;
        if (value.Value.Equals(1)
         || value.Value.Equals(true)
         || value.Value.Equals("true")
         || value.Value.Equals("yes")
         || value.Value.Equals("1"))
            return true;
        return Convert.ToBoolean(value.Value);
    }

    public byte ToByte(IFormatProvider provider) => Convert.ToByte(Value, provider);
    public static explicit operator byte(JsonValue value) => Convert.ToByte(value.Value);

    public char ToChar(IFormatProvider provider) => Convert.ToChar(Value, provider);
    public static explicit operator char(JsonValue value) => Convert.ToChar(value.Value);

    public DateTime ToDateTime(IFormatProvider provider) => Convert.ToDateTime(Value, provider);
    public static explicit operator DateTime(JsonValue value) => Convert.ToDateTime(value.Value);

    public decimal ToDecimal(IFormatProvider provider) => Convert.ToDecimal(Value, provider);
    public static explicit operator decimal(JsonValue value) => Convert.ToDecimal(value.Value);

    public double ToDouble(IFormatProvider provider) => Convert.ToDouble(Value, provider);
    public static explicit operator double(JsonValue value) => Convert.ToDouble(value.Value);

    public short ToInt16(IFormatProvider provider) => Convert.ToInt16(Value, provider);
    public static explicit operator short(JsonValue value) => Convert.ToInt16(value.Value);

    public int ToInt32(IFormatProvider provider) => Convert.ToInt32(Value, provider);
    public static explicit operator int(JsonValue value) => Convert.ToInt32(value.Value);

    public long ToInt64(IFormatProvider provider) => Convert.ToInt64(Value, provider);
    public static explicit operator long(JsonValue value) => Convert.ToInt64(value.Value);

    public sbyte ToSByte(IFormatProvider provider) => Convert.ToSByte(Value, provider);
    public static explicit operator sbyte(JsonValue value) => Convert.ToSByte(value.Value);

    public float ToSingle(IFormatProvider provider) => Convert.ToSingle(Value, provider);
    public static explicit operator float(JsonValue value) => Convert.ToSingle(value.Value);

    public override string ToString() => Value?.ToString();
    public string ToString(IFormatProvider provider) => Convert.ToString(Value, provider);
    public static explicit operator string(JsonValue value) => Convert.ToString(value.Value);

    public object ToType(Type conversionType, IFormatProvider provider) => (Value == null ? null : Convert.ChangeType(Value, conversionType, provider));

    public ushort ToUInt16(IFormatProvider provider) => Convert.ToUInt16(Value, provider);
    public static explicit operator ushort(JsonValue value) => Convert.ToUInt16(value.Value);

    public uint ToUInt32(IFormatProvider provider) => Convert.ToUInt32(Value, provider);
    public static explicit operator uint(JsonValue value) => Convert.ToUInt32(value.Value);

    public ulong ToUInt64(IFormatProvider provider) => Convert.ToUInt64(Value, provider);
    public static explicit operator ulong(JsonValue value) => Convert.ToUInt64(value.Value);

    public byte[] ToByteArray() => (Value is string ? Convert.FromBase64String((string)Value) : (byte[])Value);
    public byte[] ToByteArray(IFormatProvider provider) => (Value is string ? Convert.FromBase64String((string)Value) : (byte[])Value);

    public IJson As(string rename)
    {
        return new JsonValue
        {
            TypeCode = this.TypeCode,
            Type = this.Type,
            Name = rename,
            Value = this.Value
        };
    }

    public static explicit operator byte[](JsonValue value) => (value.Value is string ? Convert.FromBase64String((string)value.Value) : (byte[])value.Value);

}

public class Types
{
    public static readonly Type VOID = typeof(void);
    public static readonly Type OBJECT = typeof(object);
    public static readonly Type BOOL = typeof(bool);
    public static readonly Type BYTE = typeof(byte);
    public static readonly Type CHAR = typeof(char);
    public static readonly Type DATETIME = typeof(DateTime);
    public static readonly Type DECIMAL = typeof(decimal);
    public static readonly Type DOUBLE = typeof(double);
    public static readonly Type SHORT = typeof(short);
    public static readonly Type INT = typeof(int);
    public static readonly Type LONG = typeof(long);
    public static readonly Type SBYTE = typeof(sbyte);
    public static readonly Type FLOAT = typeof(float);
    public static readonly Type STRING = typeof(string);
    public static readonly Type TIMESPAN = typeof(TimeSpan);
    public static readonly Type USHORT = typeof(ushort);
    public static readonly Type UINT = typeof(uint);
    public static readonly Type ULONG = typeof(ulong);
    public static readonly Type BYTE_ARR = typeof(byte[]);
}

