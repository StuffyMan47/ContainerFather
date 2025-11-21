using ContainerFather.Infrastructure.DAL.Entites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContainerFather.Infrastructure.DAL.DbContext;

public partial class AppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<BroadcastMessage> BroadcastMessages => Set<BroadcastMessage>();
    public DbSet<Chat> Chats => Set<Chat>();
    
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TelegramId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
        
        builder.HasOne(e => e.Chat)
            .WithMany()
            .HasForeignKey(e => e.ChatId);
    }
    
    public void Configure(EntityTypeBuilder<BroadcastMessage> builder)
    {
        builder.HasKey(x => x.Id);
    }
    
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Users).WithMany(x => x.Chats);
    }
}