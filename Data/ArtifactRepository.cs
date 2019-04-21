using openrmf_save_api.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Extensions.Options;

namespace openrmf_save_api.Data {
    public class ArtifactRepository : IArtifactRepository
    {
        private readonly ArtifactContext _context = null;

        public ArtifactRepository(IOptions<Settings> settings)
        {
            _context = new ArtifactContext(settings);
        }

        public async Task<IEnumerable<Artifact>> GetAllArtifacts()
        {
            try
            {
                return await _context.Artifacts
                        .Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        private ObjectId GetInternalId(string id)
        {
            ObjectId internalId;
            if (!ObjectId.TryParse(id, out internalId))
                internalId = ObjectId.Empty;

            return internalId;
        }
        // query after Id or InternalId (BSonId value)
        //
        public async Task<Artifact> GetArtifact(string id)
        {
            try
            {
                ObjectId internalId = GetInternalId(id);
                return await _context.Artifacts
                                .Find(artifact => artifact.InternalId == internalId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        // query after body text, updated time, and header image size
        //
        public async Task<IEnumerable<Artifact>> GetArtifact(string bodyText, DateTime updatedFrom, long headerSizeLimit)
        {
            try
            {
                var query = _context.Artifacts.Find(artifact => artifact.title.Contains(bodyText) &&
                                    artifact.updatedOn >= updatedFrom);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }
        
        public async Task<Artifact> AddArtifact(Artifact item)
        {
            try
            {
                await _context.Artifacts.InsertOneAsync(item);
                return item;
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<bool> UpdateArtifact(string id, Artifact body)
        {
            var filter = Builders<Artifact>.Filter.Eq(s => s.InternalId, GetInternalId(id));
            try
            {
                body.InternalId = GetInternalId(id);
                // only save the data outside of the checklist, update the date
                var currentRecord = await _context.Artifacts.Find(artifact => artifact.InternalId == body.InternalId).FirstOrDefaultAsync();
                if (currentRecord != null){
                    body.rawChecklist = currentRecord.rawChecklist;
                    var actionResult = await _context.Artifacts.ReplaceOneAsync(filter, body);
                    return actionResult.IsAcknowledged && actionResult.ModifiedCount > 0;
                } 
                else {
                    throw new KeyNotFoundException();
                }
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }

        public async Task<bool> DeleteArtifact(string id)
        {
            var filter = Builders<Artifact>.Filter.Eq(s => s.InternalId, GetInternalId(id));
            try
            {
                Artifact art = new Artifact();
                art.InternalId = GetInternalId(id);
                // only save the data outside of the checklist, update the date
                var currentRecord = await _context.Artifacts.Find(artifact => artifact.InternalId == art.InternalId).FirstOrDefaultAsync();
                if (currentRecord != null){
                    DeleteResult actionResult = await _context.Artifacts.DeleteOneAsync(Builders<Artifact>.Filter.Eq("Id", art.InternalId));
                    return actionResult.IsAcknowledged && actionResult.DeletedCount > 0;
                } 
                else {
                    throw new KeyNotFoundException();
                }
            }
            catch (Exception ex)
            {
                // log or manage the exception
                throw ex;
            }
        }
    }
}