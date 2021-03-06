﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib.events;
using Model.User;
using Lib.helper;
using Lib.extension;

namespace Hiwjcn.Bll.User
{
    public class UserEventHandler :
        IConsumer<EntityDeleted<UserModel>>,
        IConsumer<EntityInserted<UserModel>>,
        IConsumer<EntityUpdated<UserModel>>
    {
        public void HandleEvent(EntityInserted<UserModel> eventMessage)
        {
            var json = eventMessage.Entity.ToJson();
            $"{json}".SaveInfoLog("添加");
        }

        public void HandleEvent(EntityUpdated<UserModel> eventMessage)
        {
            var json = eventMessage.Entity.ToJson();
            $"{json}".SaveInfoLog("更新");
        }

        public void HandleEvent(EntityDeleted<UserModel> eventMessage)
        {
            var json = eventMessage.Entity.ToJson();
            $"{json}".SaveInfoLog("删除");
        }
    }
}
