using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data {
    public class DataRepository : IDatingRepository {

        private readonly DataContext _context;

        public DataRepository (DataContext context) {
            this._context = context;
        }
        public void Add<T> (T entity) where T : class {
            _context.Add (entity);
        }
        public void Delete<T> (T entity) where T : class {
            _context.Remove (entity);
        }
        public async Task<Photo> GetPhoto (int id) {
            var photo = await _context.Photos.IgnoreQueryFilters()
                .FirstOrDefaultAsync (p => p.Id == id);
            return photo;
        }
        public async Task<User> GetUser (int id, bool isCurrentUser) 
        {
            var query = _context.Users.Include(p=>p.Photos).AsQueryable();
            if(isCurrentUser) {
                query = query.IgnoreQueryFilters();
            }
            var user = await query.FirstOrDefaultAsync (u => u.Id == id);
            return user;
        }
        public async Task<PagedList<User>> GetUsers (UserParams userParams) {

            var users = _context.Users.Include (p => p.Photos)
                .OrderByDescending (u => u.LastActive).AsQueryable ();

            users = users.Where (u => u.Id != userParams.Id);

            users = users.Where (u => u.Gender == userParams.Gender);

            if (userParams.Likers) {
                var userLikers = await GetUserLikes (userParams.Id, userParams.Likers);

                users = users.Where (i => userLikers.Contains (i.Id));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes (userParams.Id, userParams.Likers);

                users = users.Where (i => userLikees.Contains (i.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                var minDob = DateTime.Today.AddYears (-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears (-userParams.MinAge);

                users = users.Where (u => u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob);
            }

            if (!string.IsNullOrEmpty (userParams.OrderBy)) {
                switch (userParams.OrderBy) {
                    case "created":
                        users = users.OrderByDescending (u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending (u => u.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync (users, userParams.PageNumber, userParams.PageSize);
        }
        private async Task<IEnumerable<int>> GetUserLikes (int userId, bool likers) {

            var user = await _context.Users
                .Include (x => x.Likers)
                .Include (x => x.Likees)
                .FirstOrDefaultAsync (x => x.Id == userId);

            if (likers) {
                return user.Likers.Where (x => x.LikeeId == userId).Select (i => i.LikerId);
            } else {
                return user.Likees.Where (x => x.LikerId == userId).Select (i => i.LikeeId);
            }
        }

        public async Task<bool> SaveAll () {
            return await _context.SaveChangesAsync () > 0;
        }

        public async Task<Photo> GetMainPhotForUser (int userId) {
            return await _context.Photos.Where (u => u.UserId == userId)
                .FirstOrDefaultAsync (p => p.isMain);
        }

        public async Task<Like> GetLike (int userId, int recipientId) {
            return await _context.Likes.FirstOrDefaultAsync (u =>
                u.LikerId == userId && u.LikeeId == recipientId);
        }

        public async Task<Message> GetMessage (int messageId) {
            return await _context.Messages.FirstOrDefaultAsync (m => m.MessageId == messageId);
        }

        public async Task<PagedList<Message>> GetMessagesForUser (MessageParams messageParams) {
            var messages = _context.Messages
                .Include (p => p.Sender).ThenInclude (z => z.Photos)
                .Include (p => p.Recipient).ThenInclude (z => z.Photos)
                .AsQueryable ();

            switch (messageParams.MessageContainer) {
                case "Inbox":
                    messages = messages.Where (c => c.RecipientId == messageParams.UserId && c.RecipientDeleted == false);
                    break;
                case "Outbox":
                    messages = messages.Where (c => c.SenderId == messageParams.UserId && c.SenderDeleted == false);
                    break;
                default:
                    messages = messages.Where (c => c.RecipientId == messageParams.UserId && c.RecipientDeleted == false && c.IsRead == false);
                    break;
            }

            messages = messages.OrderByDescending (x => x.MessageSent);

            return await PagedList<Message>.CreateAsync (messages,
                messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread (int userId, int recipientId) {
            var messages = await _context.Messages
                .Include (p => p.Sender).ThenInclude (z => z.Photos)
                .Include (p => p.Recipient).ThenInclude (z => z.Photos)
                .Where (p => p.RecipientId == userId && p.RecipientDeleted == false 
                    && p.SenderId == recipientId 
                    || p.RecipientId == recipientId 
                    && p.SenderDeleted == false
                    && p.SenderId == userId)
                .OrderByDescending (q => q.MessageSent)
                .ToListAsync ();

            return messages;
        }
    }
}