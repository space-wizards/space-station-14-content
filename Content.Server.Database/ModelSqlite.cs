﻿using System;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database
{
    public sealed class SqliteServerDbContext : ServerDbContext
    {
        public DbSet<SqliteServerBan> Bans { get; set; } = default!;
        public DbSet<SqliteServerUnban> Unbans { get; set; } = default!;
        public DbSet<SqlitePlayer> Player { get; set; } = default!;
        public DbSet<SqliteConnectionLog> ConnectionLog { get; set; } = default!;

        public SqliteServerDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!InitializedWithOptions)
                options.UseSqlite("dummy connection string");
        }

        public SqliteServerDbContext(DbContextOptions<ServerDbContext> options) : base(options)
        {
        }
    }

    public class SqliteServerBan
    {
        public int Id { get; set; }

        public Guid? UserId { get; set; }
        public string? Address { get; set; }

        public DateTime BanTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string Reason { get; set; } = null!;
        public Guid? BanningAdmin { get; set; }

        public SqliteServerUnban? Unban { get; set; }
    }

    public class SqliteServerUnban
    {
        public int Id { get; set; }

        public int BanId { get; set; }
        public SqliteServerBan Ban { get; set; } = null!;

        public Guid? UnbanningAdmin { get; set; }
        public DateTime UnbanTime { get; set; }
    }

    public class SqlitePlayer
    {
        public int Id { get; set; }

        // Permanent data
        public Guid UserId { get; set; }
        public DateTime FirstSeenTime { get; set; }

        // Data that gets updated on each join.
        public string LastSeenUserName { get; set; } = null!;
        public DateTime LastSeenTime { get; set; }
        public string LastSeenAddress { get; set; } = null!;
    }

    public class SqliteConnectionLog
    {
        public int Id { get; set; }

        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime Time { get; set; }
        public String Address { get; set; } = null!;
    }
}
