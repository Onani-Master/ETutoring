﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;
using SchoolSystem.ViewModels;

namespace SchoolSystem.Controllers
{
    [Authorize(Roles = "Student, Tutor")]
    public class MessagesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessagesController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        //open chat window
        public async Task<IActionResult> ChatWindow(int groupId)
        {
            //get current user, group and existing messages
            AppUser? currentUser = await _userManager.GetUserAsync(this.User);
            Group? group = _context.Groups.Where(g => g.Id == groupId).FirstOrDefault();
            List<Message> messages = new List<Message>();
            messages = _context.Messages.Where(m => m.Group == group)
                                .Include(m => m.Sender)
                                .OrderBy(m => m.TimeStamp)
                                .ToList();
            //push everything to view, from there additional sent/recive will be handle by chat hub and client side code
            ChatWindowVM viewModel = new ChatWindowVM()
            {
                CurrentUser = currentUser,
                CurrentGroup = group,
                Messages = messages,
            };
            return View(viewModel);
        }
        [Route("messages/")]
        //open user list
        public async Task<IActionResult> ChatList()
        {
            //check if current user is student, if so lead directly to the chat window of this user's current valid group
            AppUser? currentUser = await _userManager.GetUserAsync(this.User);
            if (_userManager.GetRolesAsync(currentUser).Result.Contains("Student"))
            {
                Group? group = _context.Groups.Where(g => g.User.Contains(currentUser!) && g.IsValid).Include(g => g.User).FirstOrDefault();
                return group != null ? RedirectToAction("ChatWindow", new {groupId = group.Id}) : View("ChatListStudent", new List<ChatListVM>());
            }
            //if user is not student get list of all user that this user has group with then show them to screen
            List<ChatListVM> result = new List<ChatListVM>();
            if (currentUser != null)
            {
                List<Group> groups = _context.Groups.Where( g => g.User.Contains(currentUser!)).Include(g => g.User).ToList();
                foreach (Group group in groups)
                {
                    AppUser otherUser = group.User.Where(u => u.Id != currentUser.Id).FirstOrDefault()!;
                    ChatListVM instance;
                    try
                    {
                        instance = new ChatListVM()
                        {
                            UserName = otherUser.Name,
                            groupId = group.Id,
                            image = otherUser.Image,
                            IsValid = group.IsValid,
                        }; 
                    }
                    catch(Exception ex)
                    {
                        continue;
                    }
                    result.Add(instance);
                }
            }
            return View(result);
        }
    }
}
