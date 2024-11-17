using OpenCvSharp;

namespace Coriander;

static class Image
{
    public static bool AreMatsSimilar(Mat[] mats)
    {
        if (mats.Length < 2)
        {
            return false;
        }

        Mat part1, part2, diff;
        Scalar scalar;
        for (int i = 0; i < mats.Length - 1; i++)
        {
            part1 = mats[i];
            part2 = mats[i + 1];
            int targetCols = Math.Min(part1.Cols, part2.Cols);
            int targetRows = Math.Min(part1.Rows, part2.Rows);
            Size targetSize = new(targetCols, targetRows);
            Cv2.Resize(part1, part1, targetSize);
            Cv2.Resize(part2, part2, targetSize);

            diff = new();
            Cv2.Pow(part1 - part2, 2, diff);
            scalar = Cv2.Mean(diff);
            if (scalar.Val0 > Config.Settings.ScalarThreshold)
            {
                return false;
            }
        }

        if (mats.Length == 2)
        {
            return true;
        }

        // Compare the first and last one if there are more than 3 mats
        part1 = mats.First();
        part2 = mats.Last();
        diff = new();
        Cv2.Pow(part1 - part2, 2, diff);
        scalar = Cv2.Mean(diff);

        return scalar.Val0 < Config.Settings.ScalarThreshold;
    }

    public static bool IsImageSplittedInThree(string imagePath)
    {
        Mat image = Cv2.ImRead(imagePath);

        Mat grayImage = new Mat();
        Cv2.CvtColor(src: image, dst: grayImage, code: ColorConversionCodes.BGR2GRAY);

        /*
        Mat blurredImage = new Mat();
        Cv2.GaussianBlur(src: grayImage, dst: blurredImage, ksize: new Size(5, 5), sigmaX: 0);
        */

        //
        // Cut the image to 9 part of 3 * 3
        // 1 2 3
        // 4 5 6
        // 7 8 9
        // check the (1, 2, 3), (4, 5, 6)
        foreach (int i in new[] { 0, 1 })
        {
            Mat horizontalSliceMat = grayImage.RowRange(
                i * grayImage.Rows / 3, (i + 1) * grayImage.Rows / 3
            );

            int cols = grayImage.Cols;
            int thirdCols = cols / 3;
            int margin = Config.Settings.ImageCutMargin;
            Mat part1 = horizontalSliceMat.ColRange(margin, thirdCols - margin);
            Mat part2 = horizontalSliceMat.ColRange(thirdCols + margin, thirdCols * 2 - margin);
            Mat part3 = horizontalSliceMat.ColRange(thirdCols * 2 + margin, cols - margin);
            // Cv2.ImWrite($"{i}0.png", part1);
            // Cv2.ImWrite($"{i}1.png", part2);
            // Cv2.ImWrite($"{i}2.png", part3);

            if (!AreMatsSimilar(new[] { part1, part2, part3 }))
            {
                return false;
            }
        }

        return true;
    }
}