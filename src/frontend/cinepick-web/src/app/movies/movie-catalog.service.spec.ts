import { describe, expect, it } from 'vitest';
import { tmdbPosterUrl } from './movie-catalog.service';

describe('tmdbPosterUrl', () => {
  it('builds a trusted TMDb image URL from a relative path', () => {
    expect(tmdbPosterUrl('/poster_42.jpg', 'w342'))
      .toBe('https://image.tmdb.org/t/p/w342/poster_42.jpg');
  });

  it.each([null, '', 'https://evil.example/poster.jpg', '//evil.example/poster.jpg', '/poster?.jpg'])
  ('rejects an unsafe image path: %s', path => {
    expect(tmdbPosterUrl(path)).toBeNull();
  });
});
