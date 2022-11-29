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
            if (user == null)
            {
                throw new UserNotFoundException();
            }
            if (user.GetPassword() == Extensions.GetHash(authForm.Password, user.Salt))//TODO: whe searching in bd so bad...
            {
                var claims = new List<Claim>
                    {
                        new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
                    };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                var identity = claimsIdentity;
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
            else
            {
                throw new LoginFailedException();
            }


        }

        [Authorize]
        [HttpPatch("/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordForm passwordForm)
        {
            var user = await _db.Users.Where(x => x.Name == User.Identity.Name && x.GetPassword() == Extensions.GetHash(passwordForm.OldPassword, x.Salt)).FirstOrDefaultAsync();
            if (user == null)
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
                .Include(x => x.Questions).Select(x => new
                {
                    Name = x.Name
                })
                .FirstOrDefaultAsync();
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("/register")]
        public async Task<IActionResult> PostRegister([FromBody] AuthForm authForm)
        {
            var user = new UserModel(authForm.Login, authForm.Password, RandomNumberGenerator.GetBytes(128 / 8));

            if (_db.Users.Where(x => x.Name == authForm.Login).Count() == 0)
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
            var resultObject = await _db.Questions.Where(x => x.QuestionId == id)
                .Include(x => x.User).Include(x => x.Comments).ThenInclude(x => x.User)
                .Include(x => x.Comments).ThenInclude(x => x.ReplyComments)
                .ThenInclude(x => x.User).Include(x => x.Files)
                .Select(x => new
                {
                    Title = x.Title,
                    User = x.User.Name,
                    Section = x.Section,
                    Comments = x.Comments,
                    QuestionId = x.QuestionId,
                    Description = x.Description,
                    Files = x.GetFilesBytes(_appEnvironment)
                }).FirstOrDefaultAsync();
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
                    questions = _db.Questions
                    .Include(x => x.User).Select(x => new
                    {
                        Title = x.Title,
                        Section = x.Section,
                        Description = x.Description,
                        QuestionId = x.QuestionId,
                        UserName = x.User.Name
                    }).ToPagedList(pages, 10),
                    countPages = Math.Ceiling((double)_db.Questions.Count() / 10)
                });
            }
            else
            {
                return Ok(
                    new
                    {
                        questions = _db.Questions.Where(x => x.Section.ToLower() == filter.ToLower())
                        .Include(x => x.User).Select(x => new
                        {
                            Title = x.Title,
                            Section = x.Section,
                            Description = x.Description,
                            QuestionId = x.QuestionId,
                            UserName = x.User.Name
                        }).ToPagedList(pages, 10),
                        countPages = Math.Ceiling((double)_db.Questions.Where(x => x.Section.ToLower() == filter.ToLower()).Count() / 10)
                    });
            }
        }

        [Authorize]
        [HttpPost("/comment")]
        public async Task<IActionResult> PostComment([FromBody] CommentForm commentForm)
        {
            var comment = new CommentModel()
            {
                Date = DateTime.Now,
                Question = await _db.Questions.Where(x => x.QuestionId == commentForm.QuestionId).FirstOrDefaultAsync(),
                Text = commentForm.CommentText,
                User = await _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefaultAsync()
            };
            var ret = await _db.Comments.AddAsync(comment);
            await _db.SaveChangesAsync();
            var result = await _db.Comments.Where(x => x.Date == comment.Date && x.Question == comment.Question && x.User == comment.User)
                .Include(x => x.User).Include(x => x.ReplyComments)
                .FirstOrDefaultAsync();
            return Ok(result);
        }

        [HttpGet("/search")]
        public async Task<IActionResult> GetRequestSearch(string search)
        {
            return Ok(_db.Questions.Where(x => x.Description.Contains(search.Trim()) || x.Title.Contains(search.Trim())).Select(x => new
            {
                Description = x.Description,
                Title = x.Title,
                QuestionId = x.QuestionId
            }
            ).Take(10));
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
            var questionHandler = new QuestionModel()
            {
                Title = questionForm.Title,
                Description = questionForm.Description,
                Section = questionForm.Section,
                Tag = questionForm.Tag,
                User = await _db.Users.Where(x => x.Name == User.Identity.Name).FirstOrDefaultAsync(),
                Comments = null,
                Files = files
            };
            var questionTask = await _db.Questions.AddAsync(questionHandler);
            questionHandler = questionTask.Entity;
            await _db.SaveChangesAsync();
            return Ok(questionHandler);
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
                issuer: TokenOptions.ISSUER,
                audience: TokenOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(TokenOptions.LIFETIME)),  // действие токена истекает через 60 минут
                signingCredentials: new SigningCredentials(TokenOptions.GetSymmeetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return jwt;
        }

    }
}
