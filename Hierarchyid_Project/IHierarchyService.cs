public interface IHierarchyService
{
    void CreateRoot(Person person);
    void AddNode(Person Parent,Person Child);
    // List<string> GetFullHierarchy();
    void removeSubtree(Person person);
    void RemoveNode(Person person);
    void MoveSubTree(int OldId, int NewId);
    Dictionary<string, Person> readTree();
    int numberOfNodes();
    int numberOfLevels();
    int numberOfDescendants(int id);
}
