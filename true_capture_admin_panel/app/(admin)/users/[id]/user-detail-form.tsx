"use client";

import { useActionState } from "react";
import type { AdminUserDetail } from "@/lib/api/types";
import {
  updateUserAction,
  uploadAvatarAction,
  removeAvatarAction,
  setStatusAction,
  notifyUserAction,
  noticeUserAction,
  emailUserAction,
  deletePostAction,
  type DetailState,
} from "./actions";

const CARD =
  "bg-white dark:bg-neutral-900 rounded-xl border border-neutral-200 dark:border-neutral-800 p-6";

type PostThumb = { id: number; imageSrc: string | null; caption: string | null };

export function UserDetailView({
  user,
  avatarSrc,
  posts,
  canManage,
}: {
  user: AdminUserDetail;
  avatarSrc: string | null;
  posts: PostThumb[];
  canManage: boolean;
}) {
  const id = String(user.id);

  const [profileState, profileAction, profilePending] = useActionState<DetailState, FormData>(
    updateUserAction.bind(null, id), {});
  const [avatarState, avatarAction, avatarPending] = useActionState<DetailState, FormData>(
    uploadAvatarAction.bind(null, id), {});
  const [removeState, removeAction, removePending] = useActionState<DetailState, FormData>(
    removeAvatarAction.bind(null, id), {});
  const [statusState, statusAction, statusPending] = useActionState<DetailState, FormData>(
    setStatusAction.bind(null, id), {});

  return (
    <div className="space-y-6">
      {/* Avatar + identity + social stats */}
      <section className={`${CARD} flex gap-5 items-start`}>
        <Avatar src={avatarSrc} name={user.displayName ?? user.username} />
        <div className="flex-1 space-y-3">
          <div>
            <div className="text-lg font-medium">{user.displayName ?? user.username}</div>
            <div className="text-sm text-neutral-500">@{user.username} · {user.email}</div>
          </div>
          <div className="flex gap-5 text-sm">
            <Stat label="Posts" value={user.postsCount} />
            <Stat label="Followers" value={user.followersCount} />
            <Stat label="Following" value={user.followingCount} />
          </div>
          {canManage && (
            <div className="space-y-2">
              <form action={avatarAction} className="flex flex-wrap items-center gap-2">
                <input type="file" name="file" accept="image/png,image/jpeg,image/webp"
                  required className="text-sm" />
                <button type="submit" disabled={avatarPending}
                  className="px-3 py-1.5 rounded bg-black text-white text-sm disabled:opacity-60">
                  {avatarPending ? "Uploading…" : "Upload"}
                </button>
              </form>
              {user.avatarUrl && (
                <form action={removeAction}>
                  <button type="submit" disabled={removePending}
                    className="px-3 py-1.5 rounded border text-sm disabled:opacity-60">
                    {removePending ? "Removing…" : "Remove avatar"}
                  </button>
                </form>
              )}
              <Banner state={avatarState} />
              <Banner state={removeState} />
            </div>
          )}
        </div>
      </section>

      {/* Profile fields */}
      <section className={CARD}>
        <h2 className="text-sm font-semibold mb-3">Profile</h2>
        {canManage ? (
          <form action={profileAction} className="space-y-4">
            <label className="block">
              <span className="text-sm text-neutral-500">Display name</span>
              <input name="displayName" type="text" maxLength={80}
                defaultValue={user.displayName ?? ""}
                className="mt-1 w-full rounded border px-3 py-2 bg-transparent" />
            </label>
            <label className="block">
              <span className="text-sm text-neutral-500">Bio</span>
              <textarea name="bio" rows={4} maxLength={500} defaultValue={user.bio ?? ""}
                className="mt-1 w-full rounded border px-3 py-2 bg-transparent" />
            </label>
            <div className="flex items-center gap-3">
              <button type="submit" disabled={profilePending}
                className="px-4 py-2 rounded bg-black text-white disabled:opacity-60">
                {profilePending ? "Saving…" : "Save changes"}
              </button>
              <Banner state={profileState} />
            </div>
          </form>
        ) : (
          <dl className="text-sm space-y-2">
            <Field label="Display name" value={user.displayName ?? "—"} />
            <Field label="Bio" value={user.bio ?? "—"} />
          </dl>
        )}
      </section>

      {/* Account status */}
      <section className={CARD}>
        <h2 className="text-sm font-semibold mb-3">Account status</h2>
        <div className="flex items-center gap-3">
          <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
            user.isActive
              ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300"
              : "bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300"
          }`}>
            {user.isActive ? "Active" : "Suspended"}
          </span>
          {canManage && (
            <form action={statusAction}>
              <input type="hidden" name="isActive" value={String(!user.isActive)} />
              <button type="submit" disabled={statusPending}
                className="px-3 py-1.5 rounded border text-sm disabled:opacity-60">
                {statusPending ? "Working…" : user.isActive ? "Suspend user" : "Activate user"}
              </button>
            </form>
          )}
        </div>
        <div className="mt-2"><Banner state={statusState} /></div>
      </section>

      {/* Admin messaging */}
      {canManage && <MessagingSection id={id} />}

      {/* Posts (moderation) */}
      <section className={CARD}>
        <h2 className="text-sm font-semibold mb-3">Posts ({posts.length})</h2>
        {posts.length === 0 ? (
          <p className="text-sm text-neutral-500">This user has no posts.</p>
        ) : (
          <div className="grid grid-cols-3 sm:grid-cols-4 gap-2">
            {posts.map((p) => (
              <PostCell key={p.id} userId={id} post={p} canManage={canManage} />
            ))}
          </div>
        )}
      </section>

      {/* Read-only account info */}
      <section className={CARD}>
        <h2 className="text-sm font-semibold mb-3">Account</h2>
        <dl className="text-sm grid grid-cols-2 gap-y-2">
          <Field label="User ID" value={String(user.id)} />
          <Field label="Account type" value={user.accountType} />
          <Field label="Email verified" value={user.emailVerified ? "Yes" : "No"} />
          <Field label="Admin" value={user.isAdmin ? "Yes" : "No"} />
          <Field label="Verified creator" value={user.isVerified ? "Yes" : "No"} />
          <Field label="Google linked" value={user.hasGoogle ? "Yes" : "No"} />
          <Field label="Joined" value={new Date(user.createdAtUtc).toLocaleString()} />
          <Field label="Last login"
            value={user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString() : "—"} />
        </dl>
      </section>
    </div>
  );
}

function MessagingSection({ id }: { id: string }) {
  const [notifyState, notifyAction, notifyPending] = useActionState<DetailState, FormData>(
    notifyUserAction.bind(null, id), {});
  const [noticeState, noticeAction, noticePending] = useActionState<DetailState, FormData>(
    noticeUserAction.bind(null, id), {});
  const [emailState, emailAction, emailPending] = useActionState<DetailState, FormData>(
    emailUserAction.bind(null, id), {});

  return (
    <section className={CARD}>
      <h2 className="text-sm font-semibold mb-3">Message this user</h2>
      <div className="space-y-5">
        <MessageForm title="Push notification" action={notifyAction} pending={notifyPending}
          state={notifyState} fields={["title", "body"]} />
        <MessageForm title="In-app notice" action={noticeAction} pending={noticePending}
          state={noticeState} fields={["title", "body"]} />
        <MessageForm title="Email" action={emailAction} pending={emailPending}
          state={emailState} fields={["subject", "body"]} />
      </div>
    </section>
  );
}

function MessageForm({
  title, action, pending, state, fields,
}: {
  title: string;
  action: (formData: FormData) => void;
  pending: boolean;
  state: DetailState;
  fields: [string, string]; // [first single-line field name, body field name]
}) {
  const [head, body] = fields;
  return (
    <form action={action} className="space-y-2 border-b border-neutral-100 dark:border-neutral-800 pb-4 last:border-0 last:pb-0">
      <div className="text-sm font-medium">{title}</div>
      <input name={head} placeholder={head[0].toUpperCase() + head.slice(1)} required
        className="w-full rounded border px-3 py-2 bg-transparent text-sm" />
      <textarea name={body} rows={2} placeholder="Message" required
        className="w-full rounded border px-3 py-2 bg-transparent text-sm" />
      <div className="flex items-center gap-3">
        <button type="submit" disabled={pending}
          className="px-3 py-1.5 rounded bg-black text-white text-sm disabled:opacity-60">
          {pending ? "Sending…" : "Send"}
        </button>
        <Banner state={state} />
      </div>
    </form>
  );
}

function PostCell({
  userId, post, canManage,
}: {
  userId: string;
  post: PostThumb;
  canManage: boolean;
}) {
  const [state, action, pending] = useActionState<DetailState, FormData>(
    deletePostAction.bind(null, userId, post.id), {});

  return (
    <div className="relative aspect-square rounded overflow-hidden bg-neutral-100 dark:bg-neutral-800">
      {post.imageSrc ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={post.imageSrc} alt={post.caption ?? "post"}
          className="w-full h-full object-cover" />
      ) : (
        <div className="w-full h-full flex items-center justify-center text-neutral-400 text-xs">
          no image
        </div>
      )}
      {canManage && (
        <form action={action} className="absolute top-1 right-1">
          <button type="submit" disabled={pending} title={state.error ?? "Delete post"}
            className="w-6 h-6 rounded-full bg-black/60 text-white text-xs disabled:opacity-60">
            ×
          </button>
        </form>
      )}
    </div>
  );
}

function Banner({ state }: { state: DetailState }) {
  if (state.error) {
    return (
      <div className="text-sm text-red-600 bg-red-50 dark:bg-red-950/40 p-2 rounded">
        {state.error}
      </div>
    );
  }
  if (state.ok) {
    return (
      <div className="text-sm text-emerald-700 bg-emerald-50 dark:bg-emerald-950/40 p-2 rounded">
        {state.ok}
      </div>
    );
  }
  return null;
}

function Avatar({ src, name }: { src: string | null; name: string }) {
  if (src) {
    return (
      // eslint-disable-next-line @next/next/no-img-element
      <img src={src} alt={name}
        className="w-24 h-24 rounded-full object-cover border border-neutral-200 dark:border-neutral-800" />
    );
  }
  const initials = name.trim().split(/\s+/).slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "").join("");
  return (
    <div className="w-24 h-24 rounded-full bg-neutral-200 dark:bg-neutral-800 flex items-center justify-center text-xl font-semibold text-neutral-500">
      {initials || "?"}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div>
      <span className="font-semibold">{value}</span>{" "}
      <span className="text-neutral-500">{label}</span>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-neutral-500">{label}</dt>
      <dd className="whitespace-pre-wrap">{value}</dd>
    </div>
  );
}
