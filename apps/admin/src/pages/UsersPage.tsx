import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { fetchUsers, updateUser, deactivateUser, activateUser, deleteUser } from "../api/client";
import { Card, CardContent, CardHeader, CardTitle, Table, TableBody, TableCell, TableHead, TableHeader, TableRow, Button } from "@saas/ui";
import { useState } from "react";

interface User {
    id: string;
    email: string;
    fullName: string;
    role: string;
    isActive: boolean;
    createdAt: string;
}

export function UsersPage() {
    const queryClient = useQueryClient();
    const { data: users, isLoading } = useQuery<User[]>({ queryKey: ["users"], queryFn: fetchUsers });
    const [editingUser, setEditingUser] = useState<User | null>(null);

    const updateMutation = useMutation({
        mutationFn: ({ id, data }: { id: string; data: { fullName?: string; role?: string } }) => 
            updateUser(id, data),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["users"] });
            setEditingUser(null);
        },
    });

    const toggleActiveMutation = useMutation({
        mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
            isActive ? deactivateUser(id) : activateUser(id),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["users"] });
        },
    });

    const deleteMutation = useMutation({
        mutationFn: deleteUser,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["users"] });
        },
    });

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="grid gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-bold tracking-tight">Users</h1>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>All Users</CardTitle>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Name</TableHead>
                                <TableHead>Email</TableHead>
                                <TableHead>Role</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead>Created</TableHead>
                                <TableHead>Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {users?.map((user) => (
                                <TableRow key={user.id}>
                                    <TableCell>
                                        {editingUser?.id === user.id ? (
                                            <input
                                                className="border rounded px-2 py-1 text-sm"
                                                value={editingUser.fullName}
                                                onChange={(e) => setEditingUser({ ...editingUser, fullName: e.target.value })}
                                            />
                                        ) : (
                                            user.fullName
                                        )}
                                    </TableCell>
                                    <TableCell>{user.email}</TableCell>
                                    <TableCell>
                                        {editingUser?.id === user.id ? (
                                            <select
                                                className="border rounded px-2 py-1 text-sm"
                                                value={editingUser.role}
                                                onChange={(e) => setEditingUser({ ...editingUser, role: e.target.value })}
                                            >
                                                <option value="User">User</option>
                                                <option value="Admin">Admin</option>
                                            </select>
                                        ) : (
                                            <span className={`px-2 py-1 rounded text-xs font-medium ${
                                                user.role === 'Admin' ? 'bg-purple-100 text-purple-800' : 'bg-gray-100 text-gray-800'
                                            }`}>
                                                {user.role}
                                            </span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        <span className={`px-2 py-1 rounded text-xs font-medium ${
                                            user.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                                        }`}>
                                            {user.isActive ? 'Active' : 'Inactive'}
                                        </span>
                                    </TableCell>
                                    <TableCell className="text-sm text-gray-500">
                                        {new Date(user.createdAt).toLocaleDateString()}
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex gap-2">
                                            {editingUser?.id === user.id ? (
                                                <>
                                                    <Button
                                                        onClick={() => updateMutation.mutate({
                                                            id: user.id,
                                                            data: { fullName: editingUser.fullName, role: editingUser.role }
                                                        })}
                                                        disabled={updateMutation.isPending}
                                                    >
                                                        Save
                                                    </Button>
                                                    <Button onClick={() => setEditingUser(null)}>
                                                        Cancel
                                                    </Button>
                                                </>
                                            ) : (
                                                <>
                                                    <button
                                                        onClick={() => setEditingUser(user)}
                                                        className="text-blue-600 hover:underline text-sm"
                                                    >
                                                        Edit
                                                    </button>
                                                    <button
                                                        onClick={() => toggleActiveMutation.mutate({ id: user.id, isActive: user.isActive })}
                                                        className={`text-sm ${user.isActive ? 'text-orange-600' : 'text-green-600'} hover:underline`}
                                                    >
                                                        {user.isActive ? 'Deactivate' : 'Activate'}
                                                    </button>
                                                    <button
                                                        onClick={() => {
                                                            if (confirm('Are you sure you want to delete this user?')) {
                                                                deleteMutation.mutate(user.id);
                                                            }
                                                        }}
                                                        className="text-red-600 hover:underline text-sm"
                                                    >
                                                        Delete
                                                    </button>
                                                </>
                                            )}
                                        </div>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </div>
    );
}
