using Microsoft.EntityFrameworkCore;
using Pagination_Project.Models;

namespace Pagination_Project.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Users => Set<Usuario>();
        public DbSet<Permisos> PermissionLevels => Set<Permisos>();
        public DbSet<Libros> Libros => Set<Libros>();
        public DbSet<Asignaciones> Asignaciones => Set<Asignaciones>();
        public DbSet<Empleados> Empleados => Set<Empleados>();
        public DbSet<Evaluaciones> Evaluaciones => Set<Evaluaciones>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("ID").HasColumnType("uuid");
                entity.Property(e => e.Username).HasColumnName("Username").IsRequired();
                entity.Property(e => e.email).HasColumnName("Email");
                entity.Property(e => e.password).HasColumnName("Password").IsRequired();
                entity.Property(e => e.Name).HasColumnName("Name");
                entity.Property(e => e.lvl_Id).HasColumnName("Lvl_Id");

                entity.HasIndex(e => e.Username).IsUnique();

                entity.HasOne(e => e.Permisos)
                    .WithMany(p => p.Usuarios)
                    .HasForeignKey(e => e.lvl_Id)
                    .HasPrincipalKey(p => p.Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Empleados>(entity =>
            {
                entity.ToTable("Employees");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID").HasColumnType("uuid");
                entity.Property(e => e.Nombre).HasColumnName("Name").IsRequired();
                entity.Property(e => e.Email).HasColumnName("Email");
                entity.Property(e => e.IdEmpleado).HasColumnName("Employee_ID").IsRequired();
                entity.Property(e => e.Activo).HasColumnName("Active").IsRequired();
            });

            modelBuilder.Entity<Libros>(entity =>
            {
                entity.ToTable("Books");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("ID").HasColumnType("uuid");
                entity.Property(e => e.BookName).HasColumnName("Book_Name").IsRequired();
                entity.Property(e => e.KGENCode).HasColumnName("KGEN_Code");
                entity.Property(e => e.LSACode).HasColumnName("LSA_Code");
                entity.Property(e => e.ProofExtract).HasColumnName("Proof_Extract");
                entity.Property(e => e.FinalExtract).HasColumnName("Final_Extract");
                entity.Property(e => e.MemoExtract).HasColumnName("Memo_Extract");
                entity.Property(e => e.DirxionDate).HasColumnName("Dirxion_Date");
                entity.Property(e => e.FinalPODate).HasColumnName("Final_PO_Date");
                entity.Property(e => e.ShippingDate).HasColumnName("Shipping_Date");
                entity.Property(e => e.PubDate).HasColumnName("Pub_Date");
                entity.Property(e => e.PrintFooter).HasColumnName("Print_Footer");
                entity.Property(e => e.LegacyCoce).HasColumnName("Legacy_Code");
                entity.Property(e => e.State).HasColumnName("State");
                entity.Property(e => e.Database).HasColumnName("Database");
                entity.Property(e => e.ProductIssue).HasColumnName("Product_Issue");
                entity.Property(e => e.TrimSize).HasColumnName("Trim_Size");
                entity.Property(e => e.BindPlant).HasColumnName("Bind_Plant");
                entity.Property(e => e.SRLSuppression).HasColumnName("SRL_Suppression");
                entity.Property(e => e.NWP).HasColumnName("NWP");
            });

            modelBuilder.Entity<Permisos>(entity =>
            {
                entity.ToTable("Permission_Level");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("Id");
                entity.Property(e => e.CreateUser).HasColumnName("Create_Users");
                entity.Property(e => e.EditUser).HasColumnName("Edit_Users");
                entity.Property(e => e.DeleteUser).HasColumnName("Delete_Users");
                entity.Property(e => e.CreateBook).HasColumnName("Create_Books");
                entity.Property(e => e.EditBook).HasColumnName("Edit_Books");
                entity.Property(e => e.DeleteBook).HasColumnName("Delete_Books");
                entity.Property(e => e.AsignBook).HasColumnName("Assign_Books");
                entity.Property(e => e.BooksView).HasColumnName("Books_view");
                entity.Property(e => e.QualifyBook).HasColumnName("Qualify_Books");
                entity.Property(e => e.CreateEmployees).HasColumnName("Create_Employees");
                entity.Property(e => e.EditEmployees).HasColumnName("Edit_Employees");
                entity.Property(e => e.DeleteEmployees).HasColumnName("Delete_Employees");
                entity.Property(e => e.EditPermissionLevels).HasColumnName("Edit_Permissions_Levels");
                entity.Property(e => e.ViewAssignations).HasColumnName("View_Assignation");
                entity.Property(e => e.Name).HasColumnName("Permission_Name");
            });

            modelBuilder.Entity<Asignaciones>(entity =>
            {
                entity.ToTable("Assignments");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("uuid");

                entity.Property(e => e.IdLibro)
                    .HasColumnName("Book_Id")
                    .HasColumnType("uuid")
                    .IsRequired();

                entity.Property(e => e.IdEmpleado)
                    .HasColumnName("Employee_Id")
                    .HasColumnType("uuid")
                    .IsRequired();

                entity.HasOne(e => e.Libro)
                    .WithMany()
                    .HasForeignKey(e => e.IdLibro)
                    .HasPrincipalKey(l => l.Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Empleado)
                    .WithMany()
                    .HasForeignKey(e => e.IdEmpleado)
                    .HasPrincipalKey(emp => emp.Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<Evaluaciones>(entity =>
            {
                entity.ToTable("Evaluations");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasColumnType("uuid");

                entity.Property(e => e.TouchingRule).HasColumnName("Touching_Rule");
                entity.Property(e => e.PagesSwapped).HasColumnName("Pages_swapped");
                entity.Property(e => e.PplpWrongPlace).HasColumnName("PPLP_Wrong_Place");
                entity.Property(e => e.CouponsHeading).HasColumnName("Coupons_Heading");
                entity.Property(e => e.DoubleTruckWrongPlace).HasColumnName("DoubleTruck_Wrong_Place");
                entity.Property(e => e.FillersOutside).HasColumnName("Fillers_Outside");
                entity.Property(e => e.MissingYspFiller).HasColumnName("Missing_YSP_Filler");
                entity.Property(e => e.GradeUnder75).HasColumnName("Grade_under_75");

                entity.Property(e => e.WhpsNoAnchors).HasColumnName("WHPS_No_Anchors");
                entity.Property(e => e.WfpsNoAnchors).HasColumnName("WFPS_No_Anchors");
                entity.Property(e => e.WdqcsNoAnchors).HasColumnName("WDQCS_No_Anchors");

                entity.Property(e => e.MissingCornerAd).HasColumnName("Missing_Corner_Ad");
                entity.Property(e => e.MissingBanner).HasColumnName("Missing_Banner");
                entity.Property(e => e.MissingRandomTab).HasColumnName("Missing_Random_Tab");
                entity.Property(e => e.MissingForcedTab).HasColumnName("Missing_Forced_Tab");

                entity.Property(e => e.FileNamingIssue).HasColumnName("File_Naming_Issue");
                entity.Property(e => e.OutputWrongDate).HasColumnName("Output_Wrong_Date");
                entity.Property(e => e.WrongPitstop).HasColumnName("Wrong_Pitstop");
                entity.Property(e => e.RestaurantBleedIssue).HasColumnName("Restaurant_Bleed_Issue");
                entity.Property(e => e.WrongSigFiller).HasColumnName("Wrong_SIG_Filler");
                entity.Property(e => e.FobFolder).HasColumnName("FOB_Folder");
                entity.Property(e => e.MissingPaidItem).HasColumnName("Missing_Paid_Item");
                entity.Property(e => e.MissingSelfPromo).HasColumnName("Missing_Self_Promo");

                entity.Property(e => e.Corrections).HasColumnName("Corrections");
                entity.Property(e => e.PendingCorrections).HasColumnName("Pending_Corrections");
                entity.Property(e => e.TaskMemoWrongComment).HasColumnName("Task_Memo_Wrong_Comment");

                entity.Property(e => e.AssignationId)
                    .HasColumnName("Assignation_Id")
                    .HasColumnType("uuid")
                    .IsRequired();

                // Columnas calculadas en PostgreSQL
                entity.Property(e => e.MotifYp)
                    .HasColumnName("Motif_YP")
                    .HasColumnType("numeric(5,2)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.MotifWp)
                    .HasColumnName("Motif_WP")
                    .HasColumnType("numeric(5,2)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.InventoryReport)
                    .HasColumnName("Inventory_Report")
                    .HasColumnType("numeric(5,2)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.ProductShippingFolder)
                    .HasColumnName("Product_Shipping_Folder")
                    .HasColumnType("numeric(5,2)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.Property(e => e.TaskMemo)
                    .HasColumnName("Task_Memo")
                    .HasColumnType("numeric(5,2)")
                    .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(e => e.Asignacion)
                    .WithMany(a => a.Evaluaciones)
                    .HasForeignKey(e => e.AssignationId)
                    .HasPrincipalKey(a => a.Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}