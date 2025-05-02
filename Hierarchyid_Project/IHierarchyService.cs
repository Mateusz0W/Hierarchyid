public interface IHierarchyService
{
    void CreateRoot(string name);
    void AddNode(string parentName, string childName);
    // List<string> GetFullHierarchy();
}
