import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { fetchPlans, createPlan } from "../api/client";
import { Card, CardContent, CardHeader, CardTitle, Table, TableBody, TableCell, TableHead, TableHeader, TableRow, Button, Input } from "@saas/ui";
import { useState } from "react";

export function PlansPage() {
    const queryClient = useQueryClient();
    const { data: plans, isLoading } = useQuery({ queryKey: ["plans"], queryFn: fetchPlans });

    const [formData, setFormData] = useState({ name: "", amount: 0 });

    const mutation = useMutation({
        mutationFn: createPlan,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["plans"] });
            setFormData({ name: "", amount: 0 });
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
                <h1 className="text-2xl font-bold tracking-tight">Billing Plans</h1>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Create New Plan</CardTitle>
                </CardHeader>
                <CardContent>
                    <form onSubmit={handleSubmit} className="flex gap-4 items-end">
                        <div className="grid gap-2">
                            <label>Name</label>
                            <Input value={formData.name} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, name: e.target.value })} placeholder="Pro Plan" required />
                        </div>
                        <div className="grid gap-2">
                            <label>Amount (USD)</label>
                            <Input value={formData.amount} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, amount: Number(e.target.value) })} type="number" step="0.01" required />
                        </div>
                        <Button type="submit" disabled={mutation.isPending}>
                            {mutation.isPending ? "Creating..." : "Create Plan"}
                        </Button>
                    </form>
                    {mutation.isError && <p className="text-red-500 mt-2">Error creating plan</p>}
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>All Plans</CardTitle>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>ID</TableHead>
                                <TableHead>Name</TableHead>
                                <TableHead>Price</TableHead>
                                <TableHead>Stripe ID</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {plans?.map((plan: any) => (
                                <TableRow key={plan.id}>
                                    <TableCell className="font-mono">{plan.id}</TableCell>
                                    <TableCell>{plan.name}</TableCell>
                                    <TableCell>${plan.amount}</TableCell>
                                    <TableCell className="font-mono text-xs">{plan.stripePriceId}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>
        </div>
    );
}
