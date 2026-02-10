import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { fetchTenants, createTenant } from "../api/client";
import { Card, CardContent, CardHeader, CardTitle, Table, TableBody, TableCell, TableHead, TableHeader, TableRow, Button, Input } from "@saas/ui";
import { useState } from "react";

export function TenantsPage() {
    const queryClient = useQueryClient();
    const { data: tenants, isLoading } = useQuery({ queryKey: ["tenants"], queryFn: fetchTenants });

    const [formData, setFormData] = useState({ name: "", slug: "", email: "" });

    const mutation = useMutation({
        mutationFn: createTenant,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["tenants"] });
            setFormData({ name: "", slug: "", email: "" });
        },
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        mutation.mutate(formData);
    };

    if (isLoading) return <div>Loading...</div>;

    return (
        <div className="grid gap-6">
            <div className="flex items-center justify-between">
                <h1 className="text-2xl font-bold tracking-tight">Tenants</h1>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Onboard New Tenant</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="flex gap-4 items-end">
                        <div className="grid gap-2">
                            <label>Name</label>
                            <Input value={formData.name} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, name: e.target.value })} placeholder="Acme Inc" required />
                        </div>
                        <div className="grid gap-2">
                            <label>Slug</label>
                            <Input value={formData.slug} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, slug: e.target.value })} placeholder="acme" required />
                        </div>
                        <div className="grid gap-2">
                            <label>Email</label>
                            <Input value={formData.email} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, email: e.target.value })} placeholder="admin@acme.com" type="email" />
                        </div>
                        <Button type="submit" disabled={mutation.isPending}>
                            {mutation.isPending ? "Creating..." : "Create Tenant"}
                        </Button>
                    </form>
                    {mutation.isError && <p className="text-red-500 mt-2">Error creating tenant</p>}
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>All Tenants</CardTitle>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>ID</TableHead>
                                <TableHead>Name</TableHead>
                                <TableHead>Slug</TableHead>
                                <TableHead>Plan</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {tenants?.map((tenant: unknown) => (
                                <TableRow key={tenant.id}>
                                    <TableCell className="font-mono">{tenant.id}</TableCell>
                                    <TableCell>{tenant.name}</TableCell>
                                    <TableCell>{tenant.slug}</TableCell>
                                    <TableCell>{tenant.subscriptionPlan}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </div>
    );
}
