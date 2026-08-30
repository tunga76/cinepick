import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AccountPage } from './account-page';

describe('AccountPage submission', () => {
  for (const mode of ['login', 'register'] as const) {
    it(`prevents duplicate ${mode} requests and allows retry after failure`, async () => {
      await TestBed.configureTestingModule({
        imports: [AccountPage],
        providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
      }).compileComponents();
      const fixture = TestBed.createComponent(AccountPage);
      const http = TestBed.inject(HttpTestingController);
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
      fixture.detectChanges();
      if (mode === 'register') {
        fixture.nativeElement.querySelectorAll('[role="tab"]')[1].click();
        fixture.detectChanges();
      }
      const values: Record<string, string> = {
        email: 'user@example.test', password: 'Example-only-123!', displayName: 'Test User',
      };
      for (const input of fixture.nativeElement.querySelectorAll('input') as NodeListOf<HTMLInputElement>) {
        input.value = values[input.getAttribute('formControlName')!];
        input.dispatchEvent(new Event('input'));
      }
      const submit = () => fixture.nativeElement.querySelector('form')
        .dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
      submit();
      submit();
      const csrf = http.expectOne('/api/auth/csrf');
      fixture.detectChanges();
      for (const tab of fixture.nativeElement.querySelectorAll('[role="tab"]')) {
        expect(tab.disabled).toBe(true);
        tab.click();
      }
      expect(fixture.nativeElement.querySelector(`[id="${mode}-email"]`)).not.toBeNull();
      csrf.flush({ token: 'first' });
      submit();
      http.expectNone('/api/auth/csrf');
      http.expectOne(`/api/auth/${mode}`).flush(null, { status: 503, statusText: 'Unavailable' });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
      expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(false);
      expect(navigate).not.toHaveBeenCalled();
      submit();
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
      http.expectOne('/api/auth/csrf').flush({ token: 'retry' });
      http.expectOne(`/api/auth/${mode}`).flush({
        id: 'user', email: values['email'], displayName: 'Test User', roles: [],
      });
      expect(navigate).toHaveBeenCalledWith('/profile');
      http.verify();
    });
  }
});
