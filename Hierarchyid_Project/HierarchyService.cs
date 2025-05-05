using Microsoft.SqlServer.Types;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;


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
}
