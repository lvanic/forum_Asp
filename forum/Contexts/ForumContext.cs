using forum.Models;
using Microsoft.EntityFrameworkCore;

namespace forum.DB
{
    public class ForumContext : DbContext
    {
        public ForumContext()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }
        public DbSet<UserModel> Users { get; set; }
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<QuestionModel> Questions { get; set; }
        public DbSet<ReplyCommentModel> ReplyComments { get; set; }
        public DbSet<FileModel> Files { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=aspnet-53bc9b9d-9d6a-45d4-8429-2a2761773502;Trusted_Connection=True;MultipleActiveResultSets=true");
            //optionsBuilder.UseSqlServer("Data Source=SQL5107.site4now.net;Initial Catalog=db_a93fad_forumdb;User Id=db_a93fad_forumdb_admin;Password=912332190Egor");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CommentModel>().HasOne(p => p.User).WithMany(x => x.Comments).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ReplyCommentModel>().HasOne(p => p.User).WithMany(x => x.ReplyComments).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<ReplyCommentModel>().HasOne(p => p.RelatedComment).WithMany(x => x.ReplyComments).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CommentModel>().HasIndex(p => new { p.Date });

            modelBuilder.Entity<FileModel>().HasIndex(p => new { p.Path });

            modelBuilder.Entity<QuestionModel>().HasIndex(p => new { p.Title });

            modelBuilder.Entity<UserModel>().HasIndex(p => new { p.Name, p.Password });
            modelBuilder.Entity<UserModel>().HasIndex(p => new { p.Name });

        }
    }
}
