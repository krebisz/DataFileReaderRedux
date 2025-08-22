using System.Collections;


public class JsonArray : IJsonComplex, IEnumerable<IJson>, IEnumerable
{
    public bool IsArray => false;

    public bool IsObject => true;

    public bool IsValue => false;

    public bool IsEmpty => Elements.Count < 1;

    public int Count => Elements.Count;

    public string Name { get; set; }

    public IJson Parent { get; set; }

    public List<IJson> Elements { get; set; }

    public IJson this[int index]
    {
        get { return (index < 0 || index > Count ? null : Elements[index]); }
        set { if (index >= 0 && index < Count) Elements[index] = value; }
    }

    public IEnumerator<IJson> GetEnumerator()
    {
        return Elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Elements.GetEnumerator();
    }

    public IJson Add(IJson child)
    {
        if (child != null)
        {
            child.Name = null;
            child.Parent = this;
            Elements.Add(child);
        }

        return this;
    }

    public IJson As(string rename)
    {
        return new JsonArray
        {
            Name = rename,
            Parent = this.Parent,
            Elements = new List<IJson>(this.Elements)
        };
    }

    public bool Contains(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            for (int index = 0; index < Elements.Count; index++)
                if (text.Equals(Elements[index].ToString(), StringComparison.InvariantCultureIgnoreCase))
                    return true;
        }

        return false;
    }

}

