using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavitalevichBot;

internal class InstModuleSettings
{

    public int StoryPeriodHours { get; set; }
    public int PostsPeriodHours { get; set; }
    public int PostsCountPage { get; set; }
    public bool IsGetPosts { get; set; }
    public bool IsGetStories { get; set; }

    public static InstModuleSettings Deffault = new InstModuleSettings
    {
        StoryPeriodHours = 1,
        PostsPeriodHours = 1,
        PostsCountPage = 2,
        IsGetPosts = true,
        IsGetStories = true
    };
}

