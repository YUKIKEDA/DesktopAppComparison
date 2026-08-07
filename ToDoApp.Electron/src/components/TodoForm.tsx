import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { format } from "date-fns";
import { Input } from "./ui/Input";
import { Select } from "./ui/Select";
import { Button } from "./ui/Button";
import type { TodoItem } from "../types";

const todoSchema = z.object({
  title: z
    .string()
    .min(1, "タイトルは必須です")
    .max(200, "タイトルは200文字以内です"),
  description: z.string().max(500, "説明は500文字以内です").optional(),
  status: z.enum(["未着手", "進行中", "完了"]),
  priority: z.enum(["低", "中", "高"]),
  dueDate: z
    .string()
    .nullable()
    .optional()
    .transform((val) => (val === "" || val === undefined ? null : val)),
});

type TodoFormData = z.infer<typeof todoSchema>;

interface TodoFormProps {
  item?: TodoItem;
  onSubmit: (data: TodoFormData) => void;
  onCancel: () => void;
}

export function TodoForm({ item, onSubmit, onCancel }: TodoFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<TodoFormData>({
    resolver: zodResolver(todoSchema),
    defaultValues: item
      ? {
          title: item.title,
          description: item.description,
          status: item.status,
          priority: item.priority,
          dueDate: item.dueDate
            ? format(new Date(item.dueDate), "yyyy-MM-dd'T'HH:mm")
            : "",
        }
      : {
          title: "",
          description: "",
          status: "未着手",
          priority: "中",
          dueDate: "",
        },
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
          タイトル <span className="text-red-500">*</span>
        </label>
        <Input {...register("title")} />
        {errors.title && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.title.message}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
          説明
        </label>
        <Input {...register("description")} />
        {errors.description && (
          <p className="mt-1 text-sm text-red-600 dark:text-red-400">
            {errors.description.message}
          </p>
        )}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
            ステータス
          </label>
          <Select {...register("status")}>
            <option value="未着手">未着手</option>
            <option value="進行中">進行中</option>
            <option value="完了">完了</option>
          </Select>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
            優先度
          </label>
          <Select {...register("priority")}>
            <option value="低">低</option>
            <option value="中">中</option>
            <option value="高">高</option>
          </Select>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1 dark:text-gray-300">
          期限
        </label>
        <Input
          type="datetime-local"
          {...register("dueDate", {
            setValueAs: (v) => (v === "" ? null : new Date(v).toISOString()),
          })}
        />
      </div>

      <div className="flex justify-end gap-2 pt-4">
        <Button type="button" variant="outline" onClick={onCancel}>
          キャンセル
        </Button>
        <Button type="submit">{item ? "更新" : "追加"}</Button>
      </div>
    </form>
  );
}
