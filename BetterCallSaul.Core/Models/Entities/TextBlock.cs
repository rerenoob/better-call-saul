namespace BetterCallSaul.Core.Models.Entities;

public class TextBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid TextPageId { get; set; }
    public virtual TextPage TextPage { get; set; } = null!;
    
    public string Text { get; set; } = string.Empty;
    
    public double Confidence { get; set; }
    
    public BoundingBox? BoundingBox { get; set; }
    
    public TextBlockType Type { get; set; } = TextBlockType.Text;
    
    public Dictionary<string, object>? Properties { get; set; }
}

public class BoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public enum TextBlockType
{
    Text,
    Title,
    Header,
    Footer,
    Table,
    Image,
    Form,
    Signature,
    Barcode
}