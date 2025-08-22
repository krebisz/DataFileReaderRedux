using System.Collections;


public interface IJson
{
    bool IsArray { get; }
    bool IsObject { get; }
    bool IsValue { get; }
    bool IsEmpty { get; }
    string Name { get; set; }
    IJson Parent { get; set; }

    IJson As(string rename);
}

public interface IJsonPrimitive : IJson
{
    TypeCode TypeCode { get; }
    Type Type { get; }
    object Value { get; set; }
}
public interface IJsonComplex : IJson, IEnumerable<IJson>, IEnumerable
{
    int Count { get; }
    IJson this[int index] { get; }

    IJson Add(IJson child);
    bool Contains(string text);

}

