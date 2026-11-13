using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tutoring.Api.Features.Tutors.Exceptions;
using Tutoring.Domain.Lessons;
using Tutoring.Infrastructure.Persistence;

namespace Tutoring.Api.Features.Tutors.SaveLessonNote;

public sealed class SaveLessonNoteHandler(TutoringDbContext dbContext, ILogger<SaveLessonNoteHandler> logger)
    : IRequestHandler<SaveLessonNoteCommand>
{
    public async Task Handle(SaveLessonNoteCommand request, CancellationToken cancellationToken)
    {
        var tutorId = await dbContext.Tutors.Where(tutor => tutor.UserAccountId == request.UserAccountId)
            .Select(tutor => tutor.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (tutorId.Value == Guid.Empty)
            throw new TutorNotFoundException(request.UserAccountId.Value);

        var lessonId = new LessonId(request.LessonId);
        var lesson = await dbContext.Lessons.Include(lesson => lesson.Note)
            .SingleOrDefaultAsync(lesson => lesson.Id == lessonId && lesson.Agreement.TutorId == tutorId,
                cancellationToken);
        if (lesson is null)
            throw new LessonNotFoundException(tutorId.Value, request.LessonId);

        var note = lesson.Note;
        if (note is null)
        {
            note = new LessonNote(lessonId, request.Topic, request.CoveredTopics, request.StudentProgress);
            dbContext.LessonNotes.Add(note);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // A concurrent PUT may have inserted the note first.
                dbContext.Entry(note).State = EntityState.Detached;
                var existingNote = await dbContext.LessonNotes.SingleOrDefaultAsync(
                    existing => existing.LessonId == lessonId, cancellationToken);
                if (existingNote is null)
                    throw;

                await UpdateAsync(existingNote, request, cancellationToken);
            }
        }
        else
        {
            await UpdateAsync(note, request, cancellationToken);
        }

        logger.LogInformation("Saved note for lesson {LessonId} by tutor {TutorId}. RequestMetadata: {RequestMetadata}",
            lessonId.Value, tutorId.Value, request.RequestMetadata);
    }

    private async Task UpdateAsync(LessonNote note, SaveLessonNoteCommand request, CancellationToken cancellationToken)
    {
        note.Update(request.Topic, request.CoveredTopics, request.StudentProgress);
        var entry = dbContext.Entry(note);
        entry.Property(x => x.Topic).IsModified = true;
        entry.Property(x => x.CoveredTopics).IsModified = true;
        entry.Property(x => x.StudentProgress).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
