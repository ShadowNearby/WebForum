﻿using Ardalis.Result;
using WebForumApi.Domain.Entities.Enums;
using MediatR;

namespace WebForumApi.Application.Features.Heroes.CreateHero;

public record CreateHeroRequest : IRequest<Result<GetHeroResponse>>
{
    public string Name { get; init; } = null!;

    public string? Nickname { get; init; }
    public int? Age { get; init; }
    public string Individuality { get; init; } = null!;
    public HeroType HeroType { get; init; }

    public string? Team { get; init; }
}