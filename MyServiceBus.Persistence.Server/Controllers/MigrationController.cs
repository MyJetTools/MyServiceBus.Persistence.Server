using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyServiceBus.Persistence.Domains.MessagesContent;
using MyServiceBus.Persistence.Domains.MessagesContent.Page;

namespace MyServiceBus.Persistence.Server.Controllers
{
    
    [ApiController]
    public class MigrationController : Controller
    {
        private async ValueTask<ReadOnlyContentPage> TryToReadFromTheLegacyCompressed(string topicId,
            MessagePageId pageId)
        {

            var page = (await ServiceLocator.LegacyCompressedMessagesStorage
                .GetCompressedPageAsync(topicId, pageId))
                .ToReadOnlyContentPage();

            return page;
        }


        private async ValueTask<ReadOnlyContentPage> TryGetPageFromOtherFiles(string topicId, MessagePageId pageId, ReadOnlyContentPage page)
        {
            
            var legacyCompressedPage = await TryToReadFromTheLegacyCompressed(topicId, pageId);


            if (legacyCompressedPage != null)
            {
                if (page == null)
                    page = legacyCompressedPage;
                else
                    page.MergeWith(legacyCompressedPage);
            }

            var uncompressedPage =
                await ServiceLocator.PersistentOperationsScheduler.RestorePageAsync(topicId, false, pageId, "Migration");

            if (uncompressedPage != null)
            {

                if (page == null)
                    page = uncompressedPage.ToReadOnlyContentPage();
                else
                    page.MergeWith(uncompressedPage);
            }


            return page;
        }


        private async Task SaveAllMessagesAsync(string topicId, ReadOnlyContentPage page)
        {
            page.FilterOnlyMessagesBelongsToThePage();
            Console.WriteLine($"Enquening {topicId} page {page.PageId.Value} to compress. Messages: {page.Count}");
            await ServiceLocator.PersistentOperationsScheduler.CompressPageAsync(topicId, page, "MIGRATION");
            ServiceLocator.IndexByMinuteWriter.NewMessages(topicId, page.GetMessages());

        }
        

        private async Task GoThroughItAsync(string topicId, long pageId)
        {

            Console.WriteLine($"Migrating Page: {topicId}/{pageId}");
            
            var thePageId = new MessagePageId(pageId);

            var page =
                (await ServiceLocator.CompressedMessagesStorage
                    .GetCompressedPageAsync(topicId, thePageId))?
                    .ToReadOnlyContentPage();

            
            if (page != null)
            {
                if (page.HasAllMessages())
                {
                    Console.WriteLine("No need to do migration. Page has all the messages");
                    await ServiceLocator.LegacyCompressedMessagesStorage.DeleteIfExistsAsync(topicId, page.PageId);
                    await ServiceLocator.MessagesContentPersistentStorage.DeleteNonCompressedPageAsync(topicId,
                        page.PageId);
                    return;
                }
                Console.WriteLine("Compressed Page is found by it has: " + page.Count + " messages...");
                
            }
            else
            {
                Console.WriteLine("Compressed Page is not found. Trying to compile it...");
                
            }

            if (page != null)
            {
                if (page.Count > 100000)
                {
                    page.FilterOnlyMessagesBelongsToThePage();
                    if (page.HasAllMessages())
                    {
                        Console.WriteLine($"After Filtering we have {page.Count} messages. Saving the compressed page");
                        await SaveAllMessagesAsync(topicId, page);
                        return;
                    }
                }
                    
                
            }
            
            
            
            page = await TryGetPageFromOtherFiles(topicId, thePageId, page);

            if (page == null)
            {
                Console.WriteLine($"Page {thePageId.Value} is not found in any kind");
                return;
            }
            
            
            if (page.Count>100000)
                page.FilterOnlyMessagesBelongsToThePage();
            
            if (page.HasAllMessages())
            {
                Console.WriteLine("First iteration - All Messages are found. Saving");
                await SaveAllMessagesAsync(topicId, page);
                return;
            }


            if (thePageId.Value > 0)
            {
                var prevPageId = thePageId.PrevPage();

                Console.WriteLine("Reading Previous");
                
                var prevPage =
                    (await ServiceLocator.CompressedMessagesStorage
                        .GetCompressedPageAsync(topicId, thePageId))?
                        .ToReadOnlyContentPage();
                
                prevPage =
                    await TryGetPageFromOtherFiles(topicId, prevPageId, prevPage);
                
                page.MergeWith(prevPage);
                
                page.FilterOnlyMessagesBelongsToThePage();

                if (page.HasAllMessages())
                {
                    Console.WriteLine(
                        "After merging with Prev Messages All Messages are found. Now i would save Compressed data");
                    
                    await SaveAllMessagesAsync(topicId, page);
                    return;
                }
            }

            var nextPageId = thePageId.NextPage();

            Console.WriteLine("Reading Next");
            
            var nextPage =
                (await ServiceLocator.CompressedMessagesStorage
                    .GetCompressedPageAsync(topicId, thePageId))?
                    .ToReadOnlyContentPage();            
            nextPage =
                await TryGetPageFromOtherFiles(topicId, nextPageId, nextPage);


            page.MergeWith(nextPage);
            
            page.FilterOnlyMessagesBelongsToThePage();

            if (page.HasAllMessages())
            {
                Console.WriteLine("After merging with the Next Page Messages All Messages are found. Now i would save Compressed data");
                await SaveAllMessagesAsync(topicId, page);
                return;
            }


            Content("Now Page has messages: " + page.Count);
            
        }


        [HttpPost("migration/page")]
        public async Task<IActionResult> Page([FromQuery] string topicId, [FromQuery] long pageFrom, [FromQuery] long pageTo)
        {
            for (var pageId = pageFrom; pageId <= pageTo; pageId++)
            {
                await GoThroughItAsync(topicId, pageId);
            }

            return Content("Done");

        }
    }
}