import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { LoginComponent } from './login/login.component';
import { CreateUserComponent } from './create-user/create-user.component';
import { StoreHomeComponent } from './store/store-home/store-home.component';
import { BuyComponent } from './store/buy/buy.component';
import { SellComponent } from './store/sell/sell.component';
import { MyListingsComponent } from './store/my-listings/my-listings.component';
import { NotificationsComponent } from './store/notifications/notifications.component';
import { AdminHomeComponent } from './admin/admin-home/admin-home.component';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },

  // Auth pages
  { path: 'login', component: LoginComponent },
  { path: 'create-user', component: CreateUserComponent },

  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: 'store', component: StoreHomeComponent },

      // Buy: bare page + book-specific page
      { path: 'store/buy', component: BuyComponent },
      { path: 'store/buy/:bookId', component: BuyComponent },

      { path: 'store/sell', component: SellComponent },
      { path: 'store/my-listings', component: MyListingsComponent },
      { path: 'store/notifications', component: NotificationsComponent },

      { path: 'admin', component: AdminHomeComponent }
    ]
  },

  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
