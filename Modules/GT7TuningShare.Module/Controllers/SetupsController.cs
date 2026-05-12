using GT7TuningShare.Module.Indexes;
using GT7TuningShare.Module.Models;
using GT7TuningShare.Module.Services;
using GT7TuningShare.Module.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.Title.Models;
using YesSql;
using YesSql.Services;

namespace GT7TuningShare.Module.Controllers;

public class SetupsController : Controller
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;
    private readonly IContentItemDisplayManager _contentItemDisplayManager;
    private readonly IUpdateModelAccessor _updateModelAccessor;
    private readonly IRatingService _ratingService;
    private readonly ICommentService _commentService;
    private readonly IEngineSwapCatalog _engineSwapCatalog;

    public SetupsController(
        ISession session,
        IContentManager contentManager,
        IContentItemDisplayManager contentItemDisplayManager,
        IUpdateModelAccessor updateModelAccessor,
        IRatingService ratingService,
        ICommentService commentService,
        IEngineSwapCatalog engineSwapCatalog)
    {
        _session = session;
        _contentManager = contentManager;
        _contentItemDisplayManager = contentItemDisplayManager;
        _updateModelAccessor = updateModelAccessor;
        _ratingService = ratingService;
        _commentService = commentService;
        _engineSwapCatalog = engineSwapCatalog;
    }

    [HttpGet]
    [Route("")]
    [Route("setups")]
    public async Task<IActionResult> Index(string? carId, string? search, string sortBy = "recent", int page = 1)
    {
        const int pageSize = 20;
        if (page < 1) page = 1;

        var query = _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "CarSetup" && x.Latest && x.Published);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.DisplayText.Contains(search));
        }

        // Independent reads — run in parallel.
        var allCommentRecordsTask = _session.Query<CommentRecord, CommentIndex>().ListAsync();
        var allCarsTask = _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "Car" && x.Latest && x.Published).ListAsync();
        var totalCountTask = query.CountAsync();

        await Task.WhenAll(allCommentRecordsTask, allCarsTask, totalCountTask);

        var totalCount = totalCountTask.Result;
        var allCommentCounts = allCommentRecordsTask.Result
            .GroupBy(c => c.SetupContentItemId)
            .ToDictionary(g => g.Key, g => g.Count());
        var allCars = allCarsTask.Result;

        // Filter by selected car BEFORE pagination (CarContentItemId lives on the
        // CarSetupPart payload, not the ContentItemIndex, so we filter in-memory).
        IEnumerable<ContentItem> matched = !string.IsNullOrWhiteSpace(carId)
            ? (await query.ListAsync()).Where(s => s.As<CarSetupPart>()?.CarContentItemId == carId)
            : sortBy is "rated" or "commented" ? await query.ListAsync() : Enumerable.Empty<ContentItem>();

        IEnumerable<ContentItem> setups;
        if (matched.Any())
        {
            // We've already loaded the matching set — sort + paginate in memory.
            IEnumerable<ContentItem> sorted = sortBy switch
            {
                "rated" => matched
                    .OrderByDescending(s => s.As<RatingPart>()?.RatingCount ?? 0)
                    .ThenByDescending(s => s.As<RatingPart>()?.AverageRating ?? 0d)
                    .ThenByDescending(s => s.CreatedUtc),
                "commented" => matched
                    .OrderByDescending(s => allCommentCounts.GetValueOrDefault(s.ContentItemId, 0))
                    .ThenByDescending(s => s.CreatedUtc),
                "oldest" => matched.OrderBy(s => s.CreatedUtc),
                _ => matched.OrderByDescending(s => s.CreatedUtc),
            };
            setups = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else
        {
            // No carId filter and a date sort — paginate at the SQL level.
            setups = sortBy == "oldest"
                ? await query.OrderBy(x => x.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).ListAsync()
                : await query.OrderByDescending(x => x.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).ListAsync();
        }

        var carIds = setups
            .Select(s => s.As<CarSetupPart>()?.CarContentItemId ?? string.Empty)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToArray();

        var carById = new Dictionary<string, string>(StringComparer.Ordinal);
        var carGameIdById = new Dictionary<string, int>(StringComparer.Ordinal);
        if (carIds.Length > 0)
        {
            var cars = await _contentManager.GetAsync(carIds);
            foreach (var car in cars)
            {
                carById[car.ContentItemId] = car.DisplayText ?? string.Empty;
                carGameIdById[car.ContentItemId] = car.As<CarPart>()?.GameId ?? 0;
            }
        }

        var vm = new SetupsIndexViewModel
        {
            Setups = setups.Select(s =>
            {
                var carItemId = s.As<CarSetupPart>()?.CarContentItemId ?? "";
                var rating = s.As<RatingPart>();
                return new SetupListItem
                {
                    ContentItemId = s.ContentItemId,
                    Title = string.IsNullOrWhiteSpace(s.DisplayText) ? "(untitled)" : s.DisplayText,
                    CarDisplayText = carById.GetValueOrDefault(carItemId, "Unknown car"),
                    CarGameId = carGameIdById.GetValueOrDefault(carItemId, 0),
                    Author = s.Author,
                    CreatedUtc = s.CreatedUtc,
                    AverageRating = rating?.AverageRating ?? 0d,
                    RatingCount = rating?.RatingCount ?? 0,
                    CommentCount = allCommentCounts.GetValueOrDefault(s.ContentItemId, 0),
                };
            }).ToList(),
            AllCars = allCars
                .Select(c => new CarOption(c.ContentItemId, c.DisplayText ?? c.ContentItemId))
                .OrderBy(o => o.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CarId = carId,
            Search = search,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize]
    [Route("setups/create")]
    public async Task<IActionResult> Create()
    {
        var vm = new CreateSetupViewModel
        {
            AvailableCars = await LoadCarsAsync()
        };
        ViewBag.EngineSwapsByCar = _engineSwapCatalog.All;
        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [Route("setups/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSetupViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.AvailableCars = await LoadCarsAsync();
            ViewBag.EngineSwapsByCar = _engineSwapCatalog.All;
            return View(vm);
        }

        var item = await _contentManager.NewAsync("CarSetup");
        item.DisplayText = vm.Title;
        item.Alter<TitlePart>(p => p.Title = vm.Title);
        item.Alter<CarSetupPart>(p =>
        {
            p.CarContentItemId = vm.CarContentItemId;
            p.EngineSwap = vm.EngineSwap;

            p.PowerLevel = vm.PowerLevel;
            p.WeightReduction = vm.WeightReduction;
            p.Ballast = vm.Ballast;
            p.BallastPosition = vm.BallastPosition;
            p.PowerRestrictor = vm.PowerRestrictor;

            p.FrontTires = vm.FrontTires;
            p.RearTires = vm.RearTires;

            p.FrontDownforce = vm.FrontDownforce;
            p.RearDownforce = vm.RearDownforce;

            p.RideHeightFront = vm.RideHeightFront;
            p.AntiRollBarFront = vm.AntiRollBarFront;
            p.SpringRateFront = vm.SpringRateFront;
            p.DamperCompressionFront = vm.DamperCompressionFront;
            p.DamperExtensionFront = vm.DamperExtensionFront;
            p.CamberFront = vm.CamberFront;
            p.ToeFront = vm.ToeFront;

            p.RideHeightRear = vm.RideHeightRear;
            p.AntiRollBarRear = vm.AntiRollBarRear;
            p.SpringRateRear = vm.SpringRateRear;
            p.DamperCompressionRear = vm.DamperCompressionRear;
            p.DamperExtensionRear = vm.DamperExtensionRear;
            p.CamberRear = vm.CamberRear;
            p.ToeRear = vm.ToeRear;

            p.LSDInitialFront = vm.LSDInitialFront;
            p.LSDAccelFront = vm.LSDAccelFront;
            p.LSDBrakingFront = vm.LSDBrakingFront;
            p.LSDInitialRear = vm.LSDInitialRear;
            p.LSDAccelRear = vm.LSDAccelRear;
            p.LSDBrakingRear = vm.LSDBrakingRear;
            p.TorqueDistribution = vm.TorqueDistribution;

            p.FinalGear = vm.FinalGear;
            p.TopSpeed = vm.TopSpeed;
            p.Gear1 = vm.Gear1;
            p.Gear2 = vm.Gear2;
            p.Gear3 = vm.Gear3;
            p.Gear4 = vm.Gear4;
            p.Gear5 = vm.Gear5;
            p.Gear6 = vm.Gear6;
            p.Gear7 = vm.Gear7;

            p.BrakeBalance = vm.BrakeBalance;
            p.FrontBrakePower = vm.FrontBrakePower;
            p.RearBrakePower = vm.RearBrakePower;

            p.TractionControl = vm.TractionControl;
            p.ABS = vm.ABS;
            p.ASM = vm.ASM;

            p.Turbocharger = vm.Turbocharger;
            p.Supercharger = vm.Supercharger;
            p.AntiLagSystem = vm.AntiLagSystem;
            p.Intercooler = vm.Intercooler;

            p.AirCleaner = vm.AirCleaner;
            p.ExhaustManifold = vm.ExhaustManifold;
            p.Muffler = vm.Muffler;
            p.CatalyticConverter = vm.CatalyticConverter;

            p.BrakeSystem = vm.BrakeSystem;
            p.BrakePads = vm.BrakePads;
            p.HandbrakeType = vm.HandbrakeType;
            p.HandbrakeTorque = vm.HandbrakeTorque;

            p.ChangeSteeringAngle = vm.ChangeSteeringAngle;
            p.FourWSSystem = vm.FourWSSystem;
            p.RearSteeringAngle = vm.RearSteeringAngle;

            p.ClutchAndFlywheel = vm.ClutchAndFlywheel;
            p.PropellerShaft = vm.PropellerShaft;

            p.BoreUp = vm.BoreUp;
            p.StrokeUp = vm.StrokeUp;
            p.EngineBalanceTuning = vm.EngineBalanceTuning;
            p.PolishPorts = vm.PolishPorts;
            p.HighLiftCamShaft = vm.HighLiftCamShaft;
            p.TitaniumConnectingRodsPistons = vm.TitaniumConnectingRodsPistons;
            p.RacingCrankShaft = vm.RacingCrankShaft;
            p.HighCompressionPistons = vm.HighCompressionPistons;

            p.WeightReductionStage1 = vm.WeightReductionStage1;
            p.WeightReductionStage2 = vm.WeightReductionStage2;
            p.WeightReductionStage3 = vm.WeightReductionStage3;
            p.WeightReductionStage4 = vm.WeightReductionStage4;
            p.WeightReductionStage5 = vm.WeightReductionStage5;
            p.IncreaseBodyRigidity = vm.IncreaseBodyRigidity;

            p.Description = vm.Description;
            p.RecommendedTrack = vm.RecommendedTrack;
        });

        await _contentManager.CreateAsync(item, VersionOptions.Published);
        return RedirectToAction("Details", new { contentItemId = item.ContentItemId });
    }

    [HttpGet]
    [Authorize]
    [Route("my")]
    public async Task<IActionResult> MyActivity(string view = "all")
    {
        var userName = User.Identity?.Name ?? "";

        view = view?.ToLowerInvariant() switch
        {
            "created" => "created",
            "rated" => "rated",
            "commented" => "commented",
            _ => "all",
        };

        // Three independent queries — fan out in parallel.
        var createdItemsTask = _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "CarSetup" && x.Latest && x.Published && x.Author == userName)
            .OrderByDescending(x => x.CreatedUtc)
            .ListAsync();
        var myRatingsTask = _session.Query<RatingRecord, RatingIndex>(x => x.UserId == userName).ListAsync();
        var myCommentsTask = _session.Query<CommentRecord, CommentIndex>(x => x.UserId == userName)
            .OrderByDescending(x => x.CreatedUtc).ListAsync();

        await Task.WhenAll(createdItemsTask, myRatingsTask, myCommentsTask);

        var createdItems = createdItemsTask.Result;
        var myRatings = myRatingsTask.Result.ToList();
        var myComments = myCommentsTask.Result.ToList();

        // Most-recent-rating-date by setup, used both as the "Stars" lookup and the rated-list sort key.
        var ratingByContentItemId = myRatings
            .GroupBy(r => r.SetupContentItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedUtc).First());

        var ratedSetupIds = ratingByContentItemId.Keys.ToArray();
        var commentedIds = myComments.Select(c => c.SetupContentItemId).Distinct().ToArray();

        var ratedItems = ratedSetupIds.Length > 0
            ? (await _contentManager.GetAsync(ratedSetupIds)).Where(c => c?.ContentType == "CarSetup").ToList()
            : new List<ContentItem>();
        var commentedItems = commentedIds.Length > 0
            ? (await _contentManager.GetAsync(commentedIds)).Where(c => c?.ContentType == "CarSetup").ToList()
            : new List<ContentItem>();

        // Per-setup car info, used by both the comments and rated lists below.
        var setupInfoById = (await ToSetupListItemsAsync(commentedItems))
            .ToDictionary(s => s.ContentItemId, StringComparer.Ordinal);

        var commentedList = myComments
            .Where(c => setupInfoById.ContainsKey(c.SetupContentItemId))
            .Select(c =>
            {
                var s = setupInfoById[c.SetupContentItemId];
                return new MyCommentItem
                {
                    CommentId = c.Id,
                    SetupContentItemId = c.SetupContentItemId,
                    SetupTitle = s.Title,
                    CarDisplayText = s.CarDisplayText,
                    CarGameId = s.CarGameId,
                    Body = c.Body,
                    CreatedUtc = c.CreatedUtc,
                };
            })
            .ToList();

        var createdList = await ToSetupListItemsAsync(createdItems);

        var ratedBaseList = await ToSetupListItemsAsync(ratedItems);
        var ratedList = ratedBaseList
            .Select(s => new RatedSetupItem
            {
                ContentItemId = s.ContentItemId,
                Title = s.Title,
                CarDisplayText = s.CarDisplayText,
                CarGameId = s.CarGameId,
                Author = s.Author,
                CreatedUtc = s.CreatedUtc,
                MyStars = ratingByContentItemId.GetValueOrDefault(s.ContentItemId)?.Stars ?? 0,
            })
            .OrderByDescending(s => ratingByContentItemId.GetValueOrDefault(s.ContentItemId)?.CreatedUtc ?? DateTime.MinValue)
            .ToList();

        var vm = new MyActivityViewModel
        {
            UserName = userName,
            ActiveView = view,
            Created = createdList,
            Rated = ratedList,
            Commented = commentedList,
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize]
    [Route("setups/{contentItemId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSetup(string contentItemId, string? view = null)
    {
        var item = await _contentManager.GetAsync(contentItemId);
        if (item is null || item.ContentType != "CarSetup") return NotFound();

        var currentUser = User.Identity?.Name ?? "";
        if (!string.Equals(item.Author, currentUser, StringComparison.Ordinal))
        {
            return Forbid();
        }

        await _contentManager.RemoveAsync(item);
        return RedirectToAction("MyActivity", new { view });
    }

    [HttpPost]
    [Authorize]
    [Route("setups/{contentItemId}/unrate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unrate(string contentItemId, string? view = null)
    {
        var currentUser = User.Identity?.Name ?? "";
        if (!string.IsNullOrEmpty(currentUser))
        {
            await _ratingService.RemoveAsync(currentUser, contentItemId);
        }
        return RedirectToAction("MyActivity", new { view });
    }

    [HttpPost]
    [Authorize]
    [Route("comments/{commentId:long}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(long commentId, string? view = null)
    {
        var currentUser = User.Identity?.Name ?? "";
        if (!string.IsNullOrEmpty(currentUser))
        {
            await _commentService.RemoveByIdAsync(commentId, currentUser);
        }
        return RedirectToAction("MyActivity", new { view });
    }

    private async Task<List<SetupListItem>> ToSetupListItemsAsync(IEnumerable<ContentItem> setups)
    {
        var setupList = setups.ToList();
        var carIds = setupList
            .Select(s => s.As<CarSetupPart>()?.CarContentItemId ?? "")
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToArray();

        var carById = new Dictionary<string, string>(StringComparer.Ordinal);
        var carGameIdById = new Dictionary<string, int>(StringComparer.Ordinal);
        if (carIds.Length > 0)
        {
            var cars = await _contentManager.GetAsync(carIds);
            foreach (var car in cars)
            {
                carById[car.ContentItemId] = car.DisplayText ?? string.Empty;
                carGameIdById[car.ContentItemId] = car.As<CarPart>()?.GameId ?? 0;
            }
        }

        return setupList.Select(s =>
        {
            var carItemId = s.As<CarSetupPart>()?.CarContentItemId ?? "";
            return new SetupListItem
            {
                ContentItemId = s.ContentItemId,
                Title = string.IsNullOrWhiteSpace(s.DisplayText) ? "(untitled)" : s.DisplayText,
                CarDisplayText = carById.GetValueOrDefault(carItemId, "Unknown car"),
                CarGameId = carGameIdById.GetValueOrDefault(carItemId, 0),
                Author = s.Author,
                CreatedUtc = s.CreatedUtc,
            };
        }).ToList();
    }

    private async Task<List<CarOption>> LoadCarsAsync()
    {
        var cars = await _session.Query<ContentItem, ContentItemIndex>(
            x => x.ContentType == "Car" && x.Latest && x.Published).ListAsync();
        return cars
            .Select(c => new CarOption(
                c.ContentItemId,
                string.IsNullOrWhiteSpace(c.DisplayText) ? c.ContentItemId : c.DisplayText,
                c.As<CarPart>()?.GameId ?? 0))
            .OrderBy(o => o.DisplayText, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [HttpGet]
    [Route("setups/{contentItemId}")]
    public async Task<IActionResult> Details(string contentItemId)
    {
        if (string.IsNullOrEmpty(contentItemId))
        {
            return NotFound();
        }

        var contentItem = await _contentManager.GetAsync(contentItemId, VersionOptions.Published);
        if (contentItem is null || contentItem.ContentType != "CarSetup")
        {
            return NotFound();
        }

        var shape = await _contentItemDisplayManager.BuildDisplayAsync(contentItem, _updateModelAccessor.ModelUpdater, "Detail");

        var carPart = contentItem.As<CarSetupPart>();
        string? carDisplay = null;
        int carGameId = 0;
        if (carPart is not null && !string.IsNullOrEmpty(carPart.CarContentItemId))
        {
            var car = await _contentManager.GetAsync(carPart.CarContentItemId, VersionOptions.Published);
            carDisplay = car?.DisplayText;
            carGameId = car?.As<CarPart>()?.GameId ?? 0;
        }

        var ratingPart = contentItem.As<RatingPart>();
        int? myRating = null;
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(User.Identity.Name))
        {
            myRating = await _ratingService.GetMyRatingAsync(User.Identity.Name, contentItemId);
        }

        var comments = await _commentService.ListAsync(contentItemId);

        ViewBag.ContentItem = contentItem;
        ViewBag.CarDisplay = carDisplay;
        ViewBag.CarGameId = carGameId;
        ViewBag.AverageRating = ratingPart?.AverageRating ?? 0d;
        ViewBag.RatingCount = ratingPart?.RatingCount ?? 0;
        ViewBag.MyRating = myRating ?? 0;
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated == true;
        ViewBag.Comments = comments;

        return View("Details", shape);
    }
}
