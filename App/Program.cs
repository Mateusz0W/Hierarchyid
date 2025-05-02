
class Program
{
    static void Main()
    {
        string connectionString = "Server=.;Database=Project;TrustServerCertificate=True;Integrated Security=True;";

        var service = new HierarchyService(connectionString);

        //service.CreateRoot("CEO");

        service.AddNode("Mariusz", "Dupa");

   
    }
}
