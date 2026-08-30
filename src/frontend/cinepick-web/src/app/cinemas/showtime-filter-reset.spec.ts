import { TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { CinemaCatalogService } from './cinema-catalog.service';
import { CinemaDetailPage } from './cinema-detail-page';
import { MovieDetailPage } from '../movies/movie-detail-page';
import { MovieCatalogService } from '../movies/movie-catalog.service';
import { UserProfileService } from '../profile/user-profile.service';

class TestCinemaPage extends CinemaDetailPage {
  exerciseReset() {
    this.selectedDate.set('2026-08-30');
    this.selectedLanguage.set('tr'); this.selectedFormat.set('IMAX');
    this.maximumPrice.set(150); this.selectedPeriod.set('evening'); this.selectedSort.set('price');
    this.resetShowtimeFilters();
    return [this.selectedDate(), this.selectedLanguage(), this.selectedFormat(),
      this.maximumPrice(), this.selectedPeriod(), this.selectedSort()];
  }
}

class TestMoviePage extends MovieDetailPage {
  exerciseReset() {
    this.selectedDate.set('2026-08-30'); this.selectedCinema.set('Atlas');
    this.selectedLanguage.set('tr'); this.selectedFormat.set('IMAX');
    this.maximumPrice.set(150); this.selectedPeriod.set('evening'); this.selectedSort.set('price');
    this.resetShowtimeFilters();
    return [this.selectedDate(), this.selectedLanguage(), this.selectedFormat(),
      this.maximumPrice(), this.selectedPeriod(), this.selectedSort(), this.selectedCinema()];
  }
}

describe('showtime filter reset', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [
    { provide: ActivatedRoute, useValue: {} },
    { provide: CinemaCatalogService, useValue: {} },
    { provide: MovieCatalogService, useValue: {} },
    { provide: UserProfileService, useValue: {} },
  ] }));

  it('clears cinema filters and sorting without changing the day', () => {
    const page = TestBed.runInInjectionContext(() => new TestCinemaPage());
    expect(page.exerciseReset()).toEqual(['2026-08-30', '', '', null, 'all', 'time']);
  });

  it('also clears the cinema selection on movie details', () => {
    const page = TestBed.runInInjectionContext(() => new TestMoviePage());
    expect(page.exerciseReset()).toEqual(['2026-08-30', '', '', null, 'all', 'time', '']);
  });
});
