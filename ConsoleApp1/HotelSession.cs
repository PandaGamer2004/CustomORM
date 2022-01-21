using System;
using CustomORM;
using CustomORM.OrmLogic;

namespace ConsoleApp1
{
    
    public class Human
    {
        public Int32 Age { get; set; }
        public String Name { get; set; }
    }

    public class Cats
    {
        public String Name { get; set; }
        public String Surname { get; set; }
    }
    
    public class HotelSession : DbSession
    {
        public DbEntitySet<Human> Humans { get; set; }
        public DbEntitySet<Cats> Cats { get; set; }


        public HotelSession(string connectionString) : base(connectionString)
        {
        }
    }
}