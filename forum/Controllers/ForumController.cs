using forum.DB;
using forum.Exceptions;
using forum.Forms;
using forum.Models;
using forum.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PagedList;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace forum.Controllers
{
    [Route("[api]")]
    [ApiController]
    public class ForumController : ControllerBase
    {
        private readonly ILogger<ForumController> _logger;
        private readonly ForumContext _db;
        IWebHostEnvironment _appEnvironment;
        public ForumController(ILogger<ForumController> logger, ForumContext db, IWebHostEnvironment appEnvironment)
        {
            _logger = logger;
            _db = db;
            _appEnvironment = appEnvironment;
        }
        [AllowAnonymous]
        [HttpPost("/authorization")]
        public async Task<IActionResult> PostAuthorization([FromBody] AuthForm authForm)
        {
            var user = await _db.Users.Where(x => x.Name == authForm.Login).FirstOrDefaultAsync();
            user.GetPassword();
            Extensions.GetHash(authForm.Password, user.Salt);
            if (user != null && user.GetPassword() == Extensions.GetHash(authForm.Password, user.Salt))//TODO: whe searching in bd so bad...
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
                };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                var identity =  claimsIdentity;
                if (identity == null)
                {
                    return BadRequest();
                }
                var now = DateTime.UtcNow;
                var jwt = GetSecurityToken(authForm.Login, identity.Claims);
                var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
                var response = new
                {
                    access_token = encodedJwt,
                    login = identity.Name
                };
                return Ok(response);
            }

            throw new UserNotFoundException();
            
        }

        [Authorize]
        [HttpPatch("/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordForm passwordForm)
        {
            var user = await _db.Users.Where(x => x.Name == User.Identity.Name && x.GetPassword() == Extensions.GetHash(passwordForm.OldPassword, x.Salt)).FirstOrDefaultAsync();
            if(user == null)
            {
                return BadRequest("Неправильный пароль");
            }
            else
            {
                _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefault().Password = passwordForm.NewPassword;
                _db.SaveChangesAsync();
            }
            return Ok();
        }

        [Authorize]
        [HttpGet("/user")]
        public async Task<IActionResult> GetUser()
        {
            var result = await _db.Users.Where(x => x.Name == User.Identity.Name)
                .Include(x => x.Questions)
                .FirstOrDefaultAsync();
            //result.GetPassword();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("/register")]
        public async Task<IActionResult> PostRegister([FromBody] AuthForm authForm)
        {
            var user = new UserModel(authForm.Login, authForm.Password, RandomNumberGenerator.GetBytes(128 / 8));
            
            if(_db.Users.Where(x => x.Name == authForm.Login ).Count() == 0)
            {
                _db.Users.Add(user);
                _db.SaveChanges();
                
            }
            else
            {
                return BadRequest("Такой пользователь уже зарегистрирован");
            }

            var claims = new List<Claim>
                {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
                };

            ClaimsIdentity identity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            var jwt = GetSecurityToken(user.Name, claims);
           
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
            var response = new
            {
                access_token = encodedJwt,
                login = identity.Name
            };
            
            return Ok(response);
        }
        [Authorize]
        [HttpPost("/comment-reply")]
        public async Task<IActionResult> PostReplyComment([FromBody] ReplyCommentForm replyCommentForm)
        {
            var reply = new ReplyCommentModel()
            {
                Text = replyCommentForm.CommentText,
                RelatedComment = await _db.Comments.Where(x => x.CommentId == replyCommentForm.CommentId).FirstOrDefaultAsync(),
                User = await _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefaultAsync()
            };
            await _db.ReplyComments.AddAsync(reply);
            await _db.SaveChangesAsync();
            return Ok(reply);
        }

        [AllowAnonymous]
        [HttpGet("/question")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var result =  _db.Questions.Where(x => x.QuestionId == id)
                .Include(x => x.User).Include(x => x.Comments).ThenInclude(x => x.User)
                .Include(x => x.Comments).ThenInclude(x => x.ReplyComments).ThenInclude(x => x.User).Include(x => x.Files).FirstOrDefault();
            var files = new List<byte[]>() ;
            foreach(var file in result.Files)
            {
                var path = $"{file.Path}";
                using FileStream fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Open);
                byte[] buf = new byte[fileStream.Length];
                fileStream.Read(buf);
                files.Add(buf);
            }

            var resultObject = new
            {
                title = result.Title,
                user = result.User,
                section = result.Section,
                comments = result.Comments,
                description = result.Description,
                files = files,

            };
            return Ok(resultObject);
        }
        [HttpGet("/questions")]
        public async Task<IActionResult> GetQuestions(int? page, string filter)//todo pages
        {
            var pages = (page ?? 1);
            if (filter.ToLower() == "none")
            {
                return Ok(new 
                { 
                    questions = _db.Questions.Include(x => x.User)
                    .ToPagedList(pages, 10), 
                    countPages = Math.Ceiling((double)_db.Questions.Count() / 10) 
                });
            }
            else
            {
                return Ok(new
                {
                    questions = _db.Questions.Where(x => x.Section.ToLower() == filter.ToLower())
                    .Include(x => x.User).ToPagedList(pages, 10),
                    countPages = Math.Ceiling((double)_db.Questions.Where(x => x.Section.ToLower() == filter.ToLower()).Count() / 10)
                });
            }
        }
        
        [Authorize]
        [HttpPost("/comment")]
        public async Task<IActionResult> PostComment([FromBody]CommentForm commentForm)
        {
            var comment = new CommentModel()
            {
                Date = DateTime.Now,
                Question = await _db.Questions.Where(x => x.QuestionId == commentForm.QuestionId).FirstOrDefaultAsync(),
                Text = commentForm.CommentText,
                User = await _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefaultAsync()
            };
            await _db.Comments.AddAsync(comment);
            await _db.SaveChangesAsync();
            var result = await _db.Comments.Where(x => x.Date == comment.Date && x.Question == comment.Question && x.User == comment.User).Include(x => x.User).Include(x => x.ReplyComments).FirstOrDefaultAsync();
            return Ok(result);
        }

        [HttpGet("/search")]
        public async Task<IActionResult> GetRequestSearch(string search)
        {
            return Ok(_db.Questions.Where(x => x.Description.Contains(search.Trim()) || x.Title.Contains(search.Trim())).Take(10));
        }
        [Authorize]
        [HttpPost("/question")]
        public async Task<IActionResult> PostQuestion([FromForm] QuestionForm questionForm)
        {
             List<FileModel> files = new List<FileModel>();
            if (questionForm.FormFile != null)
            {
                foreach (var element in questionForm.FormFile)
                {
                    var path = $"/Files/{element.FileName}";

                    using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                    {
                        await element.CopyToAsync(fileStream);
                    }
                    files.Add(new FileModel() { Name = element.FileName, Path = path });
                }
            }
            var question = new QuestionModel()
            {
                Title = questionForm.Title,
                Description = questionForm.Description,
                Section = questionForm.Section,
                Tag = questionForm.Tag,
                User = await _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefaultAsync(),
                Comments = null,
                Files = files
            };
            await _db.Questions.AddAsync(question);
            await _db.SaveChangesAsync();
            return Ok(_db.Questions.Where(x => x == question).FirstOrDefault());
        }

        [AllowAnonymous]
        [HttpGet("/tags")]
        public async Task<IActionResult> GetTags(int i)
        {
            return Ok();
        }
        private JwtSecurityToken GetSecurityToken(string name, IEnumerable<Claim> claims)
        {
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),  // действие токена истекает через 60 минут
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmeetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return jwt;
        }

    }
}
