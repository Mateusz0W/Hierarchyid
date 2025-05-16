public interface IHierarchyService
{
    void CreateRoot(string name, string surname, DateTime birthDate, DateTime? deathDate);
    void AddNode(Person Parent,Person Child);
    // List<string> GetFullHierarchy();
    void removeSubtree(string name);
    void moveSubTree(string parentName, string name);
    Dictionary<string, Person> readTree();
    int numberOfNodes();
    int numberOfLevels();
    int numberOfDescendants(string parentName);
}
