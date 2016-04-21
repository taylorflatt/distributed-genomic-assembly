namespace Genome.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class test : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.GenomeModels", "UseMasurca", c => c.Boolean(nullable: false));
            AddColumn("dbo.GenomeModels", "UseSGA", c => c.Boolean(nullable: false));
            AddColumn("dbo.GenomeModels", "UseWGS", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.GenomeModels", "UseWGS");
            DropColumn("dbo.GenomeModels", "UseSGA");
            DropColumn("dbo.GenomeModels", "UseMasurca");
        }
    }
}
