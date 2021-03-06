﻿using Lib.core;
using Lib.helper;
using Lib.infrastructure;
using Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Hiwjcn.Core.Infrastructure.User
{
    public interface IUserService : IServiceBase<UserModel>
    {
        PagerData<UserModel> GetPagerList(
            string name = null, string sex = null, string email = null, string keywords = null,
            bool LoadRoleAndPrivilege = false,
            int page = 1, int pageSize = 20);

        /// <summary>
        /// 从数据库里读取用户头像
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        byte[] GetUserImage(int userID);

        /// <summary>
        /// 通过id获取user
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        UserModel GetByID(int userID);

        /// <summary>
        /// 通过id获取多个user对象
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        List<UserModel> GetUserByIDS(params int[] ids);

        /// <summary>
        /// 用户数按照性别分组统计
        /// </summary>
        /// <returns></returns>
        List<UserCountGroupBySex> GetCountGroupBySex();

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        string DeleteUser(int userID);

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="file"></param>
        /// <param name="MaxSize"></param>
        /// <returns></returns>
        string UpdateUserMask(int userID, HttpPostedFile file, string save_path);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="nick_name"></param>
        /// <param name="sex"></param>
        /// <param name="phone"></param>
        /// <param name="qq"></param>
        /// <param name="introduction"></param>
        /// <returns></returns>
        string UpdateUserInfo(UserModel updateModel);

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        string UpdateUserPass(int userID, string old_pass, string new_pass, string re_new_pass);

        /// <summary>
        /// 生成token
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sms"></param>
        /// <returns></returns>
        string CreateToken(UserModel model);

        /// <summary>
        /// 登录账户并记录登录状态
        /// </summary>
        /// <param name="email"></param>
        /// <param name="pass"></param>
        /// <param name="autoLogin"></param>
        /// <returns></returns>
        UserModel LoginByPassWord(string email, string pass, ref string msg);

        /// <summary>
        /// 通过token找到用户
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        UserModel LoginByToken(string email, string token);

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="model"></param>
        /// <param name="web_name"></param>
        /// <returns></returns>
        string Register(UserModel model, string web_name = null);

        /// <summary>
        /// 重设用户密码为随机密码，并将新密码发送到用户邮箱
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        string ResetUserPassWord(string email);
    }
}
