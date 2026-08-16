import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { App } from './app';
import { environment } from '../environments/environment';

describe('App', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/`).flush({ service: 'ppip-platform-api', phase: 'FASE 1', status: 'skeleton' });
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the PPIP title', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/`).flush({ service: 'ppip-platform-api', phase: 'FASE 1', status: 'skeleton' });
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('PPIP');
  });

  it('should show error state when the API is unreachable', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    httpMock.expectOne(`${environment.apiBaseUrl}/`).error(new ProgressEvent('error'));
    fixture.detectChanges();
    expect(fixture.componentInstance['apiStatus']()).toBe('error');
  });
});
