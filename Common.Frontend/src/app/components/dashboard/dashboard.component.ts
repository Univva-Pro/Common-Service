import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ItemService } from '../../services/item.service';
import { UserService } from '../../services/user.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommonItemResponse, CommonItemAdminResponse, CommonItemRequest } from '../../models/item.model';
import { UserResponse, UserCreateRequest } from '../../models/user.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  items: any[] = [];
  users: UserResponse[] = [];
  username = '';
  role = '';
  isAdmin = false;
  activeTab: 'items' | 'users' = 'items';

  showModal = false;
  showUserModal = false;

  newItem: CommonItemRequest = {
    name: '',
    category: '',
    price: 0,
    stockQuantity: 0
  };

  newUser: UserCreateRequest = {
    username: '',
    password: '',
    role: 'User',
    email: ''
  };

  constructor(
    private authService: AuthService,
    private itemService: ItemService,
    private userService: UserService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.username = this.authService.getUsername() || '';
    this.role = this.authService.getRole() || '';
    this.isAdmin = this.authService.isAdmin();
    
    this.loadItems();
    if (this.isAdmin) {
      this.loadUsers();
    }
  }

  setTab(tab: 'items' | 'users'): void {
    this.activeTab = tab;
  }

  loadItems(): void {
    this.itemService.getItems().subscribe({
      next: (data) => {
        this.items = data;
      },
      error: () => {
        this.authService.logout();
        this.router.navigate(['/login']);
      }
    });
  }

  loadUsers(): void {
    if (!this.isAdmin) return;
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users = data;
      },
      error: (err) => console.error('Error loading users:', err)
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  openModal(): void {
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.newItem = {
      name: '',
      category: '',
      price: 0,
      stockQuantity: 0
    };
  }

  openUserModal(): void {
    this.showUserModal = true;
  }

  closeUserModal(): void {
    this.showUserModal = false;
    this.newUser = {
      username: '',
      password: '',
      role: 'User',
      email: ''
    };
  }

  saveItem(): void {
    this.itemService.addItem(this.newItem).subscribe({
      next: () => {
        this.closeModal();
        this.loadItems();
      },
      error: (err) => console.error(err)
    });
  }

  deleteItem(id: string): void {
    this.itemService.deleteItem(id).subscribe({
      next: () => {
        this.loadItems();
      },
      error: (err) => console.error(err)
    });
  }

  saveUser(): void {
    this.userService.createUser(this.newUser).subscribe({
      next: () => {
        this.closeUserModal();
        this.loadUsers();
      },
      error: (err) => console.error('Error saving user:', err)
    });
  }

  deleteUser(id: string): void {
    this.userService.deleteUser(id).subscribe({
      next: () => {
        this.loadUsers();
      },
      error: (err) => console.error('Error deleting user:', err)
    });
  }
}
