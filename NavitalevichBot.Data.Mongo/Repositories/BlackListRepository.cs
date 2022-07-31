using NavitalevichBot.Data.Repositories;
using NavitalevichBot.Data.Mongo.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NavitalevichBot.Data.Mongo.Repositories;

internal class BlackListRepository : IBlackListRepository
{
    private readonly MongoContext _context;

    public BlackListRepository(MongoContext context)
    {
        _context = context;
    }

    public async Task AddUserToBlackList(string userName, long chatId)
    {
        var blackListItem = new BlackListItemBson
        {
            UserName = userName,
            ChatId = chatId
        };

        await _context.BlackList.InsertOneAsync(blackListItem);
    }

    public async Task<List<string>> GetBlackList(long chatId)
    {
        return (await _context.BlackList.Find(x => x.ChatId == chatId).ToListAsync())
            ?.Select(x => x.UserName).ToList();
    }
}
