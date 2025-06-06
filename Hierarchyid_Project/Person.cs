using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class Person
{
    private string name;
    private string surname;
    private DateTime? hireDate;
    private DateTime? terminationDate;
    private string position;
    private int id;
    private int level;
    private List<Person> childrens = new List<Person>();
    public void addChild(Person child)
    {
        childrens.Add(child);
    }
    public Person(string name,string surname) {
        this.name = name;
        this.surname = surname;
    }
    public Person(string Name,string Surname,DateTime? hireDate,DateTime? terminationDate,string position)
    {
        this.name=Name;
        this.surname=Surname;
        this.hireDate = hireDate;
        this.terminationDate = terminationDate;
        this.position= position;
    }
    public Person(string Name, string Surname, DateTime? hireDate, DateTime? terminationDate, int id, int level,string position)
    {
        this.name = Name;
        this.surname = Surname;
        this.hireDate = hireDate;
        this.terminationDate = terminationDate;
        this.id = id;
        this.level = level;
        this.position = position;
    }
    public override string ToString()
    {
        return name;
    }
    public List<Person> getChildrens()
    {
        return childrens;
    }
    public string getName()
    {
        return name;
    }
    public string GetSurname()
    {
        return surname;
    }
    public DateTime? GetHireDate()
    {
        return hireDate;
    }
    public DateTime? GetTerminationDate()
    {
        return terminationDate;
    }
    public int GetID()
    {
        return id;
    }
    public void SetID(int id)
    {
        this.id = id;
    }
    public string GetPosition()
    {
        return position;
    }
}


