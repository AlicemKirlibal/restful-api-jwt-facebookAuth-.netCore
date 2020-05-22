using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetBook.Contract;
using TweetBook.Contract.v1.Request;
using TweetBook.Contract.v1.Requests;
using TweetBook.Contract.v1.Responses;
using TweetBook.Domain;
using TweetBook.Extensions;
using TweetBook.Services;

namespace TweetBook.Controllers.v1
{

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class PostsController : Controller
    {

        private readonly IPostService _postservice;

        public PostsController(IPostService postService)
        {
            _postservice = postService;
        }
        [HttpGet(ApiRoutes.Posts.GetAll)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _postservice.GetPostsAsync());
        }



        [HttpGet(ApiRoutes.Posts.Get)]
        public async Task<IActionResult> Get([FromRoute]Guid postId)
        {
            var post = await _postservice.GetPostByIdAsync(postId);

            if (post == null)
                return NotFound();

            return Ok(post);

        }

        [HttpPost(ApiRoutes.Posts.Create)]
        public async Task<IActionResult> Create([FromBody] CreatePostRequest postRequest)
        {
            var post = new Post
            {
                Name = postRequest.Name,
                UserId = HttpContext.GetUserId()
            };


            await _postservice.CreatePostAsync(post);


            //https:localhost:5001 
            var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.ToUriComponent()}";

            // base nin üzerine route i yazdım api/v1/posts/id 
            var locationUri = baseUrl + "//" + ApiRoutes.Posts.Get.Replace("{postId}", post.Id.ToString());

            var response = new PostResponse { Id = post.Id };
            return Created(locationUri, response);


        }

        [HttpPut(ApiRoutes.Posts.Update)]
        public async Task<IActionResult> Update([FromRoute]Guid postId, [FromBody] UpdatePostRequest request)
        {
            var userOwnsPost = await _postservice.UserOwnsPostAsync(postId, HttpContext.GetUserId());


            if (!userOwnsPost)
            {
                return BadRequest(error: new { error = "Bu post size ait değil" });
            }

            var post = await _postservice.GetPostByIdAsync(postId);
            post.Name = request.Name;
            

            
            var updated = await _postservice.UpdatePostAsync(post);

            if (updated)
                return Ok(post);

            return NotFound();

        }

        [HttpDelete(ApiRoutes.Posts.Delete)]
        public async Task<IActionResult> Delete([FromRoute]Guid postId)
        {

            var userOwnsPost = await _postservice.UserOwnsPostAsync(postId, HttpContext.GetUserId());


            if (!userOwnsPost)
            {
                return BadRequest(error: new { error = "Bu post size ait değil" });
            }


            var deleted = await _postservice.DeletPostAsync(postId);

            if (deleted)
                return NoContent();
            
            return NotFound();


        }


       



    }
}
