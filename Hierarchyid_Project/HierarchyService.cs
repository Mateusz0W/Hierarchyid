using Microsoft.SqlServer.Types;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System;
using System.Data;


public class HierarchyService : IHierarchyService
{
    private readonly string _connectionString;


    public HierarchyService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void CreateRoot(Person person)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var command = new SqlCommand(@"
            INSERT INTO HierarchyTable (Name,Surname,BirthDate,DeathDate, Node)
            VALUES (@name,@surname,@birthDate,@deathDate, hierarchyid::GetRoot())", connection);

        command.Parameters.AddWithValue("@name", person.getName());
        command.Parameters.AddWithValue("@surname", person.GetSurname());
        command.Parameters.AddWithValue("@birthDate",person.GetBirthDate());
        var deathDateParam = new SqlParameter("@deathDate", System.Data.SqlDbType.DateTime);
        deathDateParam.Value = person.GetDeathDate().HasValue ? person.GetDeathDate().Value : DBNull.Value;
        command.Parameters.Add(deathDateParam);
        command.ExecuteNonQuery();
    }
    private SqlHierarchyId findPathWithName(string name)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var getIdCommand = new SqlCommand(@"SELECT Node
            FROM HierarchyTable 
            WHERE Name = @name", connection);

        getIdCommand.Parameters.AddWithValue("@name", name);
        var result = getIdCommand.ExecuteScalar();

        if (result == null || result == DBNull.Value)
            throw new ArgumentException($"Node with name '{name}' not found.");

        return (SqlHierarchyId)result;

    }
    private SqlHierarchyId FindLastDirectChild(SqlHierarchyId parentId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var command = new SqlCommand(@"
        SELECT TOP 1 Node
        FROM HierarchyTable
        WHERE Node.GetAncestor(1) = @parentId
        ORDER BY Node DESC", connection);

        var param = new SqlParameter("@parentId", parentId)
        {
            UdtTypeName = "HierarchyId"
        };

        command.Parameters.Add(param);
        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            throw new ArgumentException($"Highest child not found with '{parentId}' not found.");

        return (SqlHierarchyId)result;

    }

    public void AddNode(Person Parent, Person child)
    {


        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // First, get the parent node path
        var parentId = findPathWithName(Parent.getName());

        SqlHierarchyId? lastChildId = null;
        try
        {
            lastChildId = FindLastDirectChild(parentId);
        }
        catch (ArgumentException)
        {
            // Jeśli nie ma dzieci, lastChildId pozostaje null
        }

        var newNodeId = parentId.GetDescendant(
            lastChildId ?? SqlHierarchyId.Null,
            SqlHierarchyId.Null
        );
        var command = new SqlCommand(@"
            INSERT INTO HierarchyTable (Name,Surname,BirthDate,DeathDate, Node) 
            VALUES (@name,@surname,@birthDate,@deathDate, @newNodeId)", connection);

        command.Parameters.AddWithValue("@name", child.getName());
        command.Parameters.AddWithValue("@surname", child.GetSurname());
        command.Parameters.AddWithValue("@birthDate", child.GetBirthDate());
        var deathDateParam = new SqlParameter("@deathDate", System.Data.SqlDbType.DateTime);
        deathDateParam.Value = child.GetDeathDate().HasValue ? child.GetDeathDate().Value : DBNull.Value;
        command.Parameters.Add(deathDateParam);
        var nodeParam = new SqlParameter("@newNodeId", newNodeId)
        {
            UdtTypeName = "HierarchyId"
        };
        command.Parameters.Add(nodeParam);
        command.ExecuteNonQuery();
        Console.Write("Added new node");
    }
    public void removeSubtree(Person person)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var getNodeCommand = new SqlCommand(@"
            SELECT Node 
            FROM HierarchyTable 
            WHERE Name = @name;", connection);

        getNodeCommand.Parameters.AddWithValue("@name", person.getName());
        var nodeToDelete = getNodeCommand.ExecuteScalar();
        if (nodeToDelete == null)
        {
            Console.WriteLine($"Node with name '{person.getName()}' doesnt exist.");
            return;
        }
        var deleteCommand = new SqlCommand(@"
            DELETE FROM HierarchyTable 
            WHERE Node.IsDescendantOf(@nodeToDelete) = 1;", connection);

       
        var nodeParam = new SqlParameter("@nodeToDelete", nodeToDelete)
        {
            UdtTypeName = "HierarchyId"
        };
        deleteCommand.Parameters.Add(nodeParam);
        int rowsAffected = deleteCommand.ExecuteNonQuery();
        Console.WriteLine($"Deleted {rowsAffected} nodes from tree.");

    }
    public void RemoveNode(Person person)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var getNodesCommand = new SqlCommand(@"
        DECLARE @nodeToDelete hierarchyid;
        DECLARE @parentNode hierarchyid;

        SELECT @nodeToDelete = Node, @parentNode = Node.GetAncestor(1)
        FROM HierarchyTable 
        WHERE Name = @name;

        SELECT @nodeToDelete AS NodeToDelete, @parentNode AS ParentNode;",
            connection);

        getNodesCommand.Parameters.AddWithValue("@name", person.getName());

        SqlHierarchyId nodeToDelete;
        SqlHierarchyId parentNode;

        using (var reader = getNodesCommand.ExecuteReader())
        {
            if (!reader.Read())
            {
                throw new InvalidOperationException("Person not found in hierarchy");
            }

            nodeToDelete = (SqlHierarchyId)reader["NodeToDelete"];
            parentNode = (SqlHierarchyId)reader["ParentNode"];

        }

        var getChildrenCommand = new SqlCommand(@"
        SELECT Id, Node
        FROM HierarchyTable
        WHERE Node.IsDescendantOf(@nodeToDelete) = 1
        AND Node <> @nodeToDelete
        ORDER BY Node.GetLevel()", connection);

        getChildrenCommand.Parameters.Add("@nodeToDelete", SqlDbType.Udt).Value = nodeToDelete;
        getChildrenCommand.Parameters["@nodeToDelete"].UdtTypeName = "HIERARCHYID";

        var children = new List<(int Id, SqlHierarchyId OldNode)>();

        using (var reader = getChildrenCommand.ExecuteReader())
        {
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                var oldNode = (SqlHierarchyId)reader["Node"];
                children.Add((id, oldNode));
            }
        }

        // 3. Dla każdego dziecka: wygeneruj unikalną ścieżkę i zaktualizuj
        foreach (var (id, oldNode) in children)
        {
            // Pobierz ostatnie dziecko parentNode, żeby wygenerować nową ścieżkę
            var getLastChildCmd = new SqlCommand(@"
            SELECT MAX(Node) 
            FROM HierarchyTable 
            WHERE Node.GetAncestor(1) = @parentNode", connection);

            getLastChildCmd.Parameters.Add("@parentNode", SqlDbType.Udt).Value = parentNode;
            getLastChildCmd.Parameters["@parentNode"].UdtTypeName = "HIERARCHYID";

            object result = getLastChildCmd.ExecuteScalar();
            SqlHierarchyId lastChild = (result == DBNull.Value)? SqlHierarchyId.Null: (SqlHierarchyId)result;


            var getDescendantCmd = new SqlCommand(@"
            SELECT @parentNode.GetDescendant(@lastChild, NULL)", connection);

            getDescendantCmd.Parameters.Add("@parentNode", SqlDbType.Udt).Value = parentNode;
            getDescendantCmd.Parameters["@parentNode"].UdtTypeName = "HIERARCHYID";

            var lastChildParam = getDescendantCmd.Parameters.Add("@lastChild", SqlDbType.Udt);
            lastChildParam.UdtTypeName = "HIERARCHYID";
            lastChildParam.Value = (object?)lastChild ?? DBNull.Value;

            SqlHierarchyId newNode;
            using (var reader = getDescendantCmd.ExecuteReader())
            {
                if (!reader.Read())
                    throw new Exception("Failed to generate new hierarchy ID");

                newNode = (SqlHierarchyId)reader[0];
            }

            // Zaktualizuj Node w bazie
            var updateChildCmd = new SqlCommand(@"
            UPDATE HierarchyTable
            SET Node = @newNode
            WHERE Id = @id", connection);

            updateChildCmd.Parameters.AddWithValue("@id", id);
            updateChildCmd.Parameters.Add("@newNode", SqlDbType.Udt).Value = newNode;
            updateChildCmd.Parameters["@newNode"].UdtTypeName = "HIERARCHYID";

            updateChildCmd.ExecuteNonQuery();
        }

        // 4. Usuń główny node
        var deleteCommand = new SqlCommand(@"
        DELETE FROM HierarchyTable
        WHERE Node = @nodeToDelete;", connection);

        var deleteParam = deleteCommand.Parameters.Add("@nodeToDelete", SqlDbType.Udt);
        deleteParam.Value = nodeToDelete;
        deleteParam.UdtTypeName = "HIERARCHYID";
        deleteCommand.ExecuteNonQuery();
    }

    public void MoveSubTree(string newNodeName, string oldNodeName)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var command = new SqlCommand(@"
        DECLARE @oldRoot hierarchyid, @newParent hierarchyid, @newRoot hierarchyid, @lastChild hierarchyid;
        SELECT @oldRoot = Node FROM HierarchyTable WHERE Name = @OldName;
        SELECT @newParent = Node FROM HierarchyTable WHERE Name = @NewName;

        SELECT @lastChild = MAX(Node)
        FROM HierarchyTable
        WHERE Node.GetAncestor(1) = @newParent;
        SET @newRoot = @newParent.GetDescendant(@lastChild, NULL);

        UPDATE HierarchyTable
        SET Node = Node.GetReparentedValue(@oldRoot, @newRoot)
        WHERE Node.IsDescendantOf(@oldRoot) = 1;
    ", connection);

        command.Parameters.AddWithValue("@OldName", oldNodeName);
        command.Parameters.AddWithValue("@NewName", newNodeName);

        command.ExecuteNonQuery();
    }


    public Dictionary<string, Person> readTree()
    {
        Dictionary<string, Person> tree = new Dictionary<string, Person>();
        
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        
        var command = new SqlCommand(@"
            SELECT 
             Id,
             Name,
             Surname,
             BirthDate,
             DeathDate,
             Node.ToString() AS HierarchyPath,
             Node.GetLevel() AS Level,
             Node.GetAncestor(1).ToString() AS ParentNode
             FROM HierarchyTable
             ORDER BY Node",connection);

        int l = 0;
        using (SqlDataReader reader = command.ExecuteReader()) 
        {
            while (reader.Read())
            {
                int id = reader.GetInt32(reader.GetOrdinal("Id"));
                string Name = reader.GetString(reader.GetOrdinal("Name"));
                string Surname = reader.GetString(reader.GetOrdinal("Surname"));
                DateTime? BirthDate = reader.GetDateTime(reader.GetOrdinal("BirthDate"));
                DateTime? deathDate = null;
                if (!reader.IsDBNull(reader.GetOrdinal("DeathDate")))
                {
                    deathDate = reader.GetDateTime(reader.GetOrdinal("DeathDate"));
                }
                string path = reader.GetString(reader.GetOrdinal("HierarchyPath"));
                int level = reader.GetInt16(reader.GetOrdinal("Level"));
                if (l == 0)
                    tree[path] = new Person(Name,Surname,BirthDate,deathDate, id, level);
                else
                {
                    Person person = new Person(Name, Surname, BirthDate, deathDate, id, level);
                    tree[path[..^2]].addChild(person);
                    tree[path] = person;
                }
                l++;
            }
        }
        Console.WriteLine($"readed tree");
        return tree;
    }
    public int numberOfNodes()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand(@"
            SELECT COUNT(*) AS NodeCount FROM HierarchyTable;", connection);

        var result = command.ExecuteScalar();
        return (int)result;
    }
    public int numberOfLevels()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var command = new SqlCommand(@"
           SELECT COUNT(DISTINCT Node.GetLevel()) AS NumberOfLevels FROM HierarchyTable;", connection);

        var result = command.ExecuteScalar();
        return (int)result;
    }
    public int numberOfDescendants(string parentName)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var node = findPathWithName(parentName);
        var command = new SqlCommand(@"
            SELECT COUNT(*) AS DescendantCount
            FROM HierarchyTable
            WHERE Node.IsDescendantOf(@ParentNode) = 1
            AND Node <> @ParentNode;", connection);

        var nodeParam = new SqlParameter("@ParentNode", node)
        {
            UdtTypeName = "HierarchyId"
        };

        command.Parameters.Add(nodeParam);
        var result = command.ExecuteScalar();
        return (int)result;
    }


}
