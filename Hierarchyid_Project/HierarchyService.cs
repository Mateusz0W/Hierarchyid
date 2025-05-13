using Microsoft.SqlServer.Types;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System;


public class HierarchyService : IHierarchyService
{
    private readonly string _connectionString;


    public HierarchyService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void CreateRoot(string name)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var command = new SqlCommand(@"
            INSERT INTO HierarchyTable (Name, Node)
            VALUES (@name, hierarchyid::GetRoot())", connection);
        command.Parameters.AddWithValue("@name", name);
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

    public void AddNode(string parentName, string childName)
    {


        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // First, get the parent node path
        var parentId = findPathWithName(parentName);

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
            INSERT INTO HierarchyTable (Name, Node)
            VALUES (@childName, @newNodeId)", connection);

        command.Parameters.AddWithValue("@childName", childName);
        var nodeParam = new SqlParameter("@newNodeId", newNodeId)
        {
            UdtTypeName = "HierarchyId"
        };
        command.Parameters.Add(nodeParam);
        command.ExecuteNonQuery();
        Console.Write("Added new node");
    }
    public void removeSubtree(string name)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var getNodeCommand = new SqlCommand(@"
            SELECT Node 
            FROM HierarchyTable 
            WHERE Name = @name;", connection);

        getNodeCommand.Parameters.AddWithValue("@name", name);
        var nodeToDelete = getNodeCommand.ExecuteScalar();
        if (nodeToDelete == null)
        {
            Console.WriteLine($"Node with name '{name}' doesnt exist.");
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
    public void moveSubTree(string newNodeName, string oldNodeName)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var oldRoot = findPathWithName(oldNodeName);
        var newRoot = findPathWithName(newNodeName);

        var moveCommand = new SqlCommand(@"
            UPDATE HierarchyTable
            SET Node = Node.GetReparentedValue(@oldNode, @newParentNode)
            WHERE Node.IsDescendantOf(@oldNode) = 1;", connection);

        var oldNode = new SqlParameter("@oldNode", oldRoot)
        {
            UdtTypeName = "HierarchyId"
        };
        var newNode = new SqlParameter("@newParentNode", newRoot)
        {
            UdtTypeName = "HierarchyId"
        };

        moveCommand.Parameters.Add(oldNode);
        moveCommand.Parameters.Add(newNode);

        int rowsAffected = moveCommand.ExecuteNonQuery();
        Console.WriteLine($"Moved {rowsAffected} nodes.");


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
                string name = reader.GetString(reader.GetOrdinal("Name"));
                string path = reader.GetString(reader.GetOrdinal("HierarchyPath"));
                int level = reader.GetInt16(reader.GetOrdinal("Level"));
                if (l == 0)
                    tree[path] = new Person(name, id, level);
                else
                {
                    Person person = new Person(name, id, level);
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
