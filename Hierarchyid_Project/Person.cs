using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class Person
{
    private string name;
    private string surname;
    private DateTime? birthDate;
    private DateTime? deathDate;
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
    public Person(string Name,string Surname,DateTime? BirthDate,DateTime? DeathDate)
    {
        this.name=Name;
        this.surname=Surname;
        this.birthDate = BirthDate;
        this.deathDate = DeathDate;
    }
    public Person(string Name, string Surname, DateTime? BirthDate, DateTime? DeathDate, int id, int level)
    {
        this.name = Name;
        this.surname = Surname;
        this.birthDate = BirthDate;
        this.deathDate = DeathDate;
        this.id = id;
        this.level = level;
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
    public DateTime? GetBirthDate()
    {
        return birthDate;
    }
    public DateTime? GetDeathDate()
    {
        return deathDate;
    }
}


