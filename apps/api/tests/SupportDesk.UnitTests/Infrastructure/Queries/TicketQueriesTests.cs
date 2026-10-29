
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public class TicketQueriesTests
{
    [Fact]
    public async Task GetPagedAsync_FiltersTicketsByPriority()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;

        var customer = new Customer(
            "Test Customer", "test@example.com", null,
            CustomerTier.Standard, now);

        var category = new Category("General", false, false);

        db.Set<Customer>().Add(customer);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync();

        db.Set<Ticket>().AddRange(
            Ticket.Raise("TCK-001", "High priority issue", "Test description",
                customer.Id, category.Id, TicketPriority.High, now),
            Ticket.Raise("TCK-002", "Low priority issue", "Test description",
                customer.Id, category.Id, TicketPriority.Low, now)
        );

        await db.SaveChangesAsync();

        var ticketQueries = new TicketQueries(db, new FixedClock());

        var result = await ticketQueries.GetPagedAsync(
            new TicketQuery { Priority = TicketPriority.High },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(TicketPriority.High, result.Items[0].Priority);
    }


    [Fact]
    public async Task GetPagedAsync_FiltersTicketsByStatus()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;

        var customer = new Customer(
            "Test Customer", "test@example.com", null,
            CustomerTier.Standard, now);

        var category = new Category("General", false, false);

        db.Set<Customer>().Add(customer);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync();

        var newTicket = Ticket.Raise(
            "TCK-001", "New issue", "Test description",
            customer.Id, category.Id, TicketPriority.Medium, now);

        var inProgressTicket = Ticket.Raise(
            "TCK-002", "Ongoing issue", "Test description",
            customer.Id, category.Id, TicketPriority.High, now);

        inProgressTicket.ChangeStatus(TicketStatus.InProgress, now);

        db.Set<Ticket>().AddRange(newTicket, inProgressTicket);
        await db.SaveChangesAsync();

        var ticketQueries = new TicketQueries(db, new FixedClock());

        var result = await ticketQueries.GetPagedAsync(
            new TicketQuery { Status = TicketStatus.InProgress },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(TicketStatus.InProgress, result.Items[0].Status);
    }


    [Fact]
    public async Task GetPagedAsync_AppliesMultipleFilters()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;

        var customer = new Customer(
            "Test Customer", "test@example.com", null,
            CustomerTier.Standard, now);

        var category = new Category("General", false, false);

        db.Set<Customer>().Add(customer);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync();

        var matchingTicket = Ticket.Raise(
            "TCK-001", "Important ongoing issue", "Test description",
            customer.Id, category.Id, TicketPriority.High, now);

        matchingTicket.ChangeStatus(TicketStatus.InProgress, now);

        var otherTicket = Ticket.Raise(
            "TCK-002", "Another high priority issue", "Test description",
            customer.Id, category.Id, TicketPriority.High, now);

        db.Set<Ticket>().AddRange(matchingTicket, otherTicket);
        await db.SaveChangesAsync();

        var ticketQueries = new TicketQueries(db, new FixedClock());

        var result = await ticketQueries.GetPagedAsync(
            new TicketQuery
            {
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress
            },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("TCK-001", result.Items[0].Reference);
    }


    [Fact]
    public async Task GetPagedAsync_PaginatesFilteredTickets()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;

        var customer = new Customer(
            "Test Customer", "test@example.com", null,
            CustomerTier.Standard, now);

        var category = new Category("General", false, false);

        db.Set<Customer>().Add(customer);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync();

        for (var i = 1; i <= 5; i++)
        {
            db.Set<Ticket>().Add(Ticket.Raise(
                $"TCK-{i:000}",
                $"High priority issue {i}",
                "Test description",
                customer.Id,
                category.Id,
                TicketPriority.High,
                now));
        }

        db.Set<Ticket>().Add(Ticket.Raise(
            "TCK-006",
            "Low priority issue",
            "Test description",
            customer.Id,
            category.Id,
            TicketPriority.Low,
            now));

        await db.SaveChangesAsync();

        var ticketQueries = new TicketQueries(db, new FixedClock());

        var result = await ticketQueries.GetPagedAsync(
            new TicketQuery
            {
                Priority = TicketPriority.High,
                Page = 2,
                PageSize = 2
            },
            CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items,
            ticket => Assert.Equal(TicketPriority.High, ticket.Priority));
    }


    [Fact]
    public async Task GetPagedAsync_FiltersTicketsBySearch()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SupportDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var now = FixedClock.DefaultNow;

        var customer = new Customer(
            "Test Customer", "test@example.com", null,
            CustomerTier.Standard, now);

        var category = new Category("General", false, false);

        db.Set<Customer>().Add(customer);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync();

        db.Set<Ticket>().AddRange(
            Ticket.Raise(
                "TCK-001", "Login page not working", "Test description",
                customer.Id, category.Id, TicketPriority.High, now),
            Ticket.Raise(
                "TCK-002", "Payment failed", "Test description",
                customer.Id, category.Id, TicketPriority.Medium, now)
        );

        await db.SaveChangesAsync();

        var ticketQueries = new TicketQueries(db, new FixedClock());

        var result = await ticketQueries.GetPagedAsync(
            new TicketQuery { Search = "Login" },
            CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("TCK-001", result.Items[0].Reference);
    }


}
