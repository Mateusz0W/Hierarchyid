using Microsoft.Data.SqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Tests.Tests
{
    public class IntegrationTest1
    {
        private readonly string _testConnectionString ="Server=localhost;Database=TestDb;Trusted_Connection=True;TrustServerCertificate=True;";
        
        [Fact]
        public void TestCreateRoot()
        {
            var service = new HierarchyService(_testConnectionString);
            var person = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(person);

            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            var command = new SqlCommand("SELECT COUNT(*) FROM HierarchyTable WHERE Name = @name", connection);
            command.Parameters.AddWithValue("@name", "Janusz");

            int count = (int)command.ExecuteScalar();
            ClearTable();
            Assert.True(count > 0);
        }

        [Fact]
        public void TestAddNode()
        {
            var service = new HierarchyService(_testConnectionString);
            var parent = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);
            var child = new Person("Adam", "Nowak", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1));

            service.CreateRoot(parent);

            int id = FindID("Janusz");
            parent.SetID(id);
            service.AddNode(parent, child);

            string expectedPath = "/1/";

            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            var command = new SqlCommand("Select Node.ToString() FROM HierarchyTable Where Name = @name ", connection);
            command.Parameters.AddWithValue("@name", "Adam");

            string realPath = (string)command.ExecuteScalar();
            ClearTable();
            Assert.Equal(expectedPath, realPath);
        }

        [Fact]
        public void TestRemoveSubtree()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);
            int id = FindID("Janusz");
            root.SetID(id);

            var parent = new Person("Micha³", "Nowak", new DateTime(1980, 2, 2), null);

            service.AddNode(root, parent);
            id = FindID("Micha³");
            parent.SetID(id);

            var child = new Person("Adam", "Nowak", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1),id,1);

            service.AddNode(parent, child);
            service.removeSubtree(parent);

            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            var command = new SqlCommand("SELECT COUNT(*) FROM HierarchyTable", connection);

            int count = (int)command.ExecuteScalar();
            ClearTable();
            Assert.True(count == 1);
        }

        [Fact]
        public void TestRemoveNode()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);
            int id = FindID("Janusz");
            root.SetID(id);

            var child = new Person("Adam", "Nowak", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1), id, 1);

            service.AddNode(root, child);
            id = FindID("Adam");
            child.SetID(id);
            service.RemoveNode(child);

            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            var command = new SqlCommand("SELECT COUNT(*) FROM HierarchyTable", connection);

            int count = (int)command.ExecuteScalar();
            ClearTable();
            Assert.Equal(1, count);
        }

        [Fact] 
        public void TestNumberOfNodes()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);

            int num = service.numberOfNodes();
            ClearTable();
            Assert.Equal(1, num);
        }

        [Fact]
        public void TestNumberOfLevels()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);

            int num = service.numberOfLevels();
            ClearTable();
            Assert.Equal(1, num);
        }

        [Fact]
        public void TestNumberOfDescendants()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);
            int id = FindID("Janusz");
            root.SetID(id);

            var parent = new Person("Micha³", "Nowak", new DateTime(1980, 2, 2), null);

            service.AddNode(root, parent);
            id = FindID("Micha³");
            parent.SetID(id);

            var child = new Person("Adam", "Nowak", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1), id, 1);

            service.AddNode(parent, child);

            int num=service.numberOfDescendants(root.GetID());
            ClearTable();
            Assert.Equal(2, num);
           
        }

        [Fact]
        public void TestMoveSubtree()
        {
            var service = new HierarchyService(_testConnectionString);
            var root = new Person("Janusz", "Nowak", new DateTime(1980, 1, 1), null);

            service.CreateRoot(root);
            int id = FindID("Janusz");
            root.SetID(id);

            var child1 = new Person("Micha³", "Nowak", new DateTime(1980, 2, 2), null);

            service.AddNode(root, child1);
            id = FindID("Micha³");
            child1.SetID(id);

            var child2 = new Person("Adam", "Nowak", new DateTime(1980, 1, 1), new DateTime(2000, 1, 1), id, 1);

            service.AddNode(child1, child2);
            id = FindID("Adam");
            child1.SetID(id);

            service.MoveSubTree(child1.GetID(), child2.GetID());
            int num = service.numberOfLevels();

            ClearTable();
            Assert.Equal(3, num);
        }
        private void ClearTable()
        {
            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();
            var cmd = new SqlCommand("DELETE FROM HierarchyTable", connection);
            cmd.ExecuteNonQuery();
        }
        private int FindID(string name)
        {
            using var connection = new SqlConnection(_testConnectionString);
            connection.Open();

            var command = new SqlCommand("Select Id FROM HierarchyTable Where Name = @name ", connection);
            command.Parameters.AddWithValue("@name", name);
            object? result = command.ExecuteScalar();
            int id = Convert.ToInt32(result);
            return id;
        }

    }
}
