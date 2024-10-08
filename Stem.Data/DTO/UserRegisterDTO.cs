﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stem.Data.DTO
{
    public class UserRegisterDTO
    {
        [EmailAddress(ErrorMessage = "Bạn phải dùng email để đăng kí tài khoản!")]
        public string? Email { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required!")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
        ErrorMessage = "Mật khẩu phải dài ít nhất 8 ký tự và bao gồm một chữ cái viết hoa, một chữ cái viết thường, một chữ số và một ký tự đặc biệt.")]
        public string? Password { get; set; }
    }
}
