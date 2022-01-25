using System;
using System.Collections.Generic;
using CustomORM.Attributes;
using CustomORM.OrmLogic;

namespace ConsoleApp1
{


    public class Field
    {
        [PrimaryKey]
        public Guid FieldId { get; set; }
        
        public Int32 FieldSize { get; set; }

        public IList<Point> Points { get; set; }
    }


    public class Point
    {
        [PrimaryKey]
        [DbColumnName("PointId")]
        public Guid Id { get; set; }
        
        public Int32 X { get; set; }
        public Int32 Y { get; set; }

        [ForeignKey("Ship")]
        public Guid ShipId { get; set; }
        [ForeignKey("Field")]
        public Guid FieldId { get; set; }
        
        
        public Ship Ship { get; set; }
        public Field Field { get; set; }
    }

    public class Ship
    {
        [PrimaryKey]
        public Guid ShipId { get; set; }
        public Single ShipSpeed { get; set; }
        [ColumnType("real")]
        public Double ActivityRadius { get; set; }
        
        public Int32 ShipLength { get; set; }
        
        [ForeignKey("ShipType")]
        public Guid TypeId { get; set; }
        
        public ShipType ShipType { get; set; }
        
        public IList<Point> Points { get; set; }
        
    }

    public class ShipType
    {
        [PrimaryKey]
        public Guid TypeId { get; set; }
        public String TypeName { get; set; }

        public IList<Ship> Ships { get; set; }
    }
    
    
    public class SeaBattleSession : DbSession
    {
        public DbEntitySet<ShipType> ShipTypes { get; set; }
        public DbEntitySet<Ship> Ships { get; set; }
        public DbEntitySet<Point> Points { get; set; }
        public DbEntitySet<Field> Fields { get; set; }
        
        public SeaBattleSession(string connectionString) : base(connectionString)
        {
        }
    }
}