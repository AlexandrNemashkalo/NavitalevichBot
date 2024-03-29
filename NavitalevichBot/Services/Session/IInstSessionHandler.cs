﻿using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.SessionHandlers;

namespace NavitalevichBot.Services.Session;

public interface IInstSessionHandler : ISessionHandler
{
    public long ChatId { get; }
    StateData GetStateData();
}
