using System.Text.Json.Serialization;

namespace WorkManager.Models.Tree;

/// <summary>
/// Test class for testing tree traversal and job completion
/// </summary>
public class StringTreeNode
{
    [JsonPropertyName("value")] 
    public String Value { get; set; }
    [JsonPropertyName("children")] 
    public List<StringTreeNode>? Children { get; set; }

    public StringTreeNode(string value)
    {
        Value = value;
    }

    public void AddChild(StringTreeNode child)
    {
        if (Children == null)
        {
            Children = new List<StringTreeNode>();
        }
        Children.Add(child);
    }
}