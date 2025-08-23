import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Cam } from './cam';

describe('Cam', () => {
  let component: Cam;
  let fixture: ComponentFixture<Cam>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Cam]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Cam);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
