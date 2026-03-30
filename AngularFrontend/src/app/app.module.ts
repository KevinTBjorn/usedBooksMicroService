import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { LoginComponent } from './login/login.component';
import { StoreHomeComponent } from './store/store-home/store-home.component';
import { AdminHomeComponent } from './admin/admin-home/admin-home.component';
import { BuyComponent } from './store/buy/buy.component';
import { SellComponent } from './store/sell/sell.component';
import { MyListingsComponent } from './store/my-listings/my-listings.component';
import { NotificationsComponent } from './store/notifications/notifications.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    StoreHomeComponent,
    AdminHomeComponent,
    BuyComponent,
    SellComponent,
    MyListingsComponent,
    NotificationsComponent,
    CreateUserComponent,
    MainLayoutComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    ReactiveFormsModule,
    RouterModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {}
