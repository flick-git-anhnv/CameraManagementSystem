using Minio;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Kztek.Object
{
    /// <summary>
    /// Dịch vụ lưu trữ ảnh bãi đỗ xe trên MinIO object storage.
    /// Cần cấu hình <see cref="EndPoint"/>, <see cref="AccessKey"/>, <see cref="SecretKey"/> trước khi sử dụng.
    /// </summary>
    public static class MinioService
    {
        /// <summary>Địa chỉ endpoint của MinIO server (ví dụ: "192.168.1.100:9000").</summary>
        public static string EndPoint = string.Empty;

        /// <summary>Access key (username) xác thực MinIO.</summary>
        public static string AccessKey = string.Empty;

        /// <summary>Secret key (password) xác thực MinIO.</summary>
        public static string SecretKey = string.Empty;

        /// <summary>Dùng HTTPS (<c>true</c>) hay HTTP (<c>false</c>) khi kết nối MinIO. Mặc định <c>false</c>.</summary>
        public static bool secure = false;

        /// <summary>Tên bucket lưu ảnh bãi đỗ xe. Mặc định "parking-images".</summary>
        public static string bucketName = "parking-images";

        /// <summary>Thư mục (prefix) con trong bucket để phân loại ảnh theo địa điểm/bãi.</summary>
        public static string folderName = "";

        /// <summary>
        /// Tạo URL presigned (có thời hạn 24 giờ) để tải ảnh từ MinIO về.
        /// </summary>
        /// <param name="key">Object key (đường dẫn object trong bucket, ví dụ: "/folder/image.jpg").</param>
        /// <returns>URL presigned để tải ảnh, hoặc <see cref="string.Empty"/> nếu có lỗi.</returns>
        public static async Task<string> GetImage(string key)
        {
            try
            {
                MinioClient minio = new MinioClient()
                            .WithEndpoint(EndPoint)
                            .WithCredentials(AccessKey, SecretKey)
                            .WithSSL(secure)
                            .Build()
                            .WithRegion("us-west-rack");
                var getListBucketsTask = await minio.ListBucketsAsync().ConfigureAwait(false);
                PresignedGetObjectArgs args = new PresignedGetObjectArgs()
                                        .WithBucket(bucketName)
                                        .WithObject(key)
                                        .WithExpiry(60 * 60 * 24);
                string url = await minio.PresignedGetObjectAsync(args);
                return url;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        //public static Stream ToStream(this Image image, ImageFormat format)
        //{
        //    var stream = new System.IO.MemoryStream();
        //    image.Save(stream, format);
        //    stream.Position = 0;
        //    return stream;
        //}
        //public static async Task<string> UploadPicture(Image? image, string imageKey)
        //{
        //    try
        //    {
        //        if (image == null)
        //        {
        //            return string.Empty;
        //        }
        //        MinioClient minio = new MinioClient()
        //                    .WithEndpoint(EndPoint)
        //                    .WithCredentials(AccessKey, SecretKey)
        //                    .WithSSL(secure)
        //                    .Build()
        //                    .WithRegion("us-west-rack");

        //        BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
        //        bool bucketExists = await minio.BucketExistsAsync(bucketExistsArgs);
        //        if (!bucketExists)
        //        {
        //            MakeBucketArgs makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
        //            await minio.MakeBucketAsync(makeBucketArgs);
        //        }

        //        Stream data = image.ToStream(ImageFormat.Jpeg);
        //        PutObjectArgs putObjectArgs = new PutObjectArgs()
        //            .WithBucket(bucketName)
        //            .WithStreamData(data)
        //            .WithObjectSize(data.Length)
        //            .WithObject(imageKey)
        //            //.WithContentType("application/octet-stream");
        //            .WithContentType("image/jpeg");

        //        var response = await minio.PutObjectAsync(putObjectArgs);
        //        return imageKey;
        //    }
        //    catch (Exception ex)
        //    {
        //        return string.Empty;
        //    }
        //}
        /// <summary>
        /// Upload file ảnh sự kiện bãi đỗ xe lên MinIO.
        /// </summary>
        /// <param name="fileName">Tên file ảnh sẽ lưu trong bucket (object key). Trả về <c>false</c> nếu null/rỗng.</param>
        /// <param name="filePath">Đường dẫn file ảnh cục bộ trên máy.</param>
        /// <param name="logTime">Thời điểm ghi nhận (dùng để tổ chức thư mục theo ngày nếu cần).</param>
        /// <returns><c>true</c> nếu upload thành công, <c>false</c> nếu thất bại.</returns>
        public static async Task<bool> UploadPictureFile(string? fileName, string filePath, DateTime logTime)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return false;
                }
                MinioClient minio = new MinioClient()
                            .WithEndpoint(EndPoint)
                            .WithCredentials(AccessKey, SecretKey)
                            .WithSSL(secure)
                            .Build()
                            .WithRegion("us-west-rack");

                BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
                bool bucketExists = await minio.BucketExistsAsync(bucketExistsArgs);
                if (!bucketExists)
                {
                    MakeBucketArgs makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                    await minio.MakeBucketAsync(makeBucketArgs);
                }
                PutObjectArgs putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithFileName(filePath)
                //.WithObject( $@"/{logTime.Year}_{logTime.Month}_{logTime.Day}/" + fileName)
                .WithObject($@"/{folderName}/" + fileName)
                    .WithContentType("image/jpeg");
                var response = await minio.PutObjectAsync(putObjectArgs);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Upload file (log, report) lên MinIO, tổ chức theo tên máy và ngày.
        /// Object key sẽ có dạng: <c>support/{machineName}/{year}_{month}_{day}/{fileName}</c>.
        /// </summary>
        /// <param name="fileName">Tên file sẽ lưu trong bucket. Trả về <c>false</c> nếu null/rỗng.</param>
        /// <param name="filePath">Đường dẫn file cục bộ trên máy.</param>
        /// <param name="machineName">Tên máy/làn xe (dùng để tạo thư mục phân loại).</param>
        /// <param name="logTime">Thời điểm log (dùng để tạo thư mục theo ngày).</param>
        /// <returns><c>true</c> nếu upload thành công, <c>false</c> nếu thất bại.</returns>
        public static async Task<bool> UploadFile(string? fileName, string filePath, string machineName, DateTime logTime)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return false;
                }
                MinioClient minio = new MinioClient()
                            .WithEndpoint(EndPoint)
                            .WithCredentials(AccessKey, SecretKey)
                            .WithSSL(secure)
                            .Build()
                            .WithRegion("us-west-rack");

                BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(bucketName);
                bool bucketExists = await minio.BucketExistsAsync(bucketExistsArgs);
                if (!bucketExists)
                {
                    MakeBucketArgs makeBucketArgs = new MakeBucketArgs().WithBucket(bucketName);
                    await minio.MakeBucketAsync(makeBucketArgs);
                }
                PutObjectArgs putObjectArgs = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithFileName(filePath)
                .WithObject("support/" + machineName + $@"/{logTime.Year}_{logTime.Month}_{logTime.Day}/" + fileName)
                    .WithContentType("text/plain");
                var response = await minio.PutObjectAsync(putObjectArgs);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
