public interface IHierarchyService
{
    void CreateRoot(string name);
    void AddNode(string parentName, string childName);
    // List<string> GetFullHierarchy();
    void removeSubtree(string name);
    void moveSubTree(string parentName, string name);
    void readTree();
    int numberOfNodes();
    int numberOfLevels();
    int numberOfDescendants(string parentName);
}
