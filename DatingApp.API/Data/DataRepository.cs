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
            var photo = await _context.Photos.FirstOrDefaultAsync (p => p.Id == id);
            return photo;
        }
        public async Task<User> GetUser (int id) {
            var user = await _context.Users.Include (p => p.Photos).FirstOrDefaultAsync (u => u.UserId == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers (UserParams userParams) {

            var users = _context.Users.Include (p => p.Photos)
                .OrderByDescending (u => u.LastActive).AsQueryable ();

            users = users.Where (u => u.UserId != userParams.UserId);

            users = users.Where (u => u.Gender == userParams.Gender);

            if (userParams.Likers) {
                var userLikers = await GetUserLikes (userParams.UserId, userParams.Likers);

                users = users.Where (i => userLikers.Contains (i.UserId));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes (userParams.UserId, userParams.Likers);

                users = users.Where (i => userLikees.Contains (i.UserId));
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
                .FirstOrDefaultAsync (x => x.UserId == userId);

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

    }
}