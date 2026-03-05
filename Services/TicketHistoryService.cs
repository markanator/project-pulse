using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Data;
using TheBugTracker.Models;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class TicketHistoryService : ITicketHistoryService
    {
        private readonly ApplicationDbContext _context;

        public TicketHistoryService(ApplicationDbContext context)
        {
            this._context = context;
        }
        public async Task AddHistoryAsync(Ticket oldTicket, Ticket newTicket, string userId)
        {
            // new ticket being added
            if (oldTicket == null && newTicket != null)
            {
                var history = new TicketHistory()
                {
                    TicketId = newTicket.Id,
                    Property = "",
                    OldValue = "",
                    Created = DateTimeOffset.UtcNow,
                    UserId = userId,
                    Description = "New Ticket Created"
                };

                try
                {
                    await _context.TicketHistories.AddAsync(history);
                    await _context.SaveChangesAsync();

                }
                catch (Exception)
                {

                    throw;
                }
            }
            else
            {
                // check ticket title
                if (oldTicket.Title != newTicket.Title)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "Title",
                        OldValue = oldTicket.Title,
                        NewValue = newTicket.Title,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket title: {newTicket.Title}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // check description
                if (oldTicket.Description != newTicket.Description)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "Description",
                        OldValue = oldTicket.Description,
                        NewValue = newTicket.Description,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket Description: {newTicket.Description}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // check TicketPriorityId
                if (oldTicket.TicketPriorityId != newTicket.TicketPriorityId)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "TicketPriority",
                        OldValue = oldTicket.TicketPriority.Name,
                        NewValue = newTicket.TicketPriority.Name,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket Ticket Priority: {newTicket.TicketPriority.Name}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // check TicketStatusId
                if (oldTicket.TicketStatusId != newTicket.TicketStatusId)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "TicketStatus",
                        OldValue = oldTicket.TicketStatus.Name,
                        NewValue = newTicket.TicketStatus.Name,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket Ticket Status: {newTicket.TicketStatus.Name}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // check TicketTypeId
                if (oldTicket.TicketTypeId != newTicket.TicketTypeId)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "TicketType",
                        OldValue = oldTicket.TicketType.Name,
                        NewValue = newTicket.TicketType.Name,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket Ticket Type: {newTicket.TicketType.Name}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                // check TicketTypeId
                if (oldTicket.DeveloperUserId != newTicket.DeveloperUserId)
                {
                    var history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        Property = "Developer",
                        OldValue = oldTicket.DeveloperUser?.FullName ?? "Not Assigned",
                        NewValue = newTicket.DeveloperUser?.FullName,
                        Created = DateTimeOffset.UtcNow,
                        UserId = userId,
                        Description = $"New Ticket Dev: {newTicket.DeveloperUser?.FullName}"
                    };

                    try
                    {
                        await _context.TicketHistories.AddAsync(history);
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                }

                try
                {
                    // add all change records to the database
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
        public async Task AddHistoryAsync(int ticketId, string model, string userId)
        {
            try
            {
                // for comments and attachements
                var ticket = await _context.Tickets.FindAsync(ticketId);
                string description = model.ToLower().Replace("ticket", "");

                TicketHistory history = new TicketHistory()
                {
                    TicketId = ticketId,
                    Property = model,
                    OldValue = "",
                    NewValue = "",
                    Created = DateTimeOffset.UtcNow,
                    UserId = userId,
                    Description = $"New {description} added to ticket: {ticket.Title}"
                };

                await _context.TicketHistories.AddAsync(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<List<TicketHistory>> GetCompanyTicketsHisotriesAsync(int companyId)
        {
            try
            {
                var projects = (await _context.Companies.Include(c => c.Projects)
                                                        .ThenInclude(p => p.Tickets)
                                                            .ThenInclude(t => t.History)
                                                                .ThenInclude(h => h.User)
                                                        .FirstOrDefaultAsync(c => c.Id == companyId)).Projects.ToList();

                var tickets = projects.SelectMany(p => p.Tickets).ToList();

                return tickets.SelectMany(t => t.History).ToList();

            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task<List<TicketHistory>> GetProjectTicketsHisotriesAsync(int projectId, int companyId)
        {
            try
            {
                var project = await _context.Projects.Where(p => p.CompanyId == companyId)
                                                        .Include(p => p.Tickets)
                                                            .ThenInclude(t => t.History)
                                                                .ThenInclude(h => h.User)
                                                        .FirstOrDefaultAsync(p => p.Id == projectId);

                return project.Tickets.SelectMany(t => t.History).ToList();

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
